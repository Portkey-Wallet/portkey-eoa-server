using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography;
using AElf.Types;
using EoaServer.AuthServer.Options;
using EoaServer.Commons;
using EoaServer.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Volo.Abp.Caching;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;
using IdentityUser = Volo.Abp.Identity.IdentityUser;
using SignInResult = Microsoft.AspNetCore.Mvc.SignInResult;

namespace EoaServer;

public class SignatureGrantHandler : ITokenExtensionGrant
{
    private IDistributedEventBus _distributedEventBus;
    private ILogger<SignatureGrantHandler> _logger;
    private IAbpDistributedLock _distributedLock;
    private IDistributedCache<string> _distributedCache;
    private readonly string _lockKeyPrefix = "EoaServer:Auth:SignatureGrantHandler:";

    public async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        var publicKeyVal = context.Request.GetParameter("pubkey").ToString();
        var signatureVal = context.Request.GetParameter("signature").ToString();
        var timestampVal = context.Request.GetParameter("timestamp").ToString();

        var invalidParamResult = CheckParams(publicKeyVal, signatureVal, timestampVal);
        if (invalidParamResult != null)
        {
            return invalidParamResult;
        }

        var publicKey = ByteArrayHelper.HexStringToByteArray(publicKeyVal);
        var signature = ByteArrayHelper.HexStringToByteArray(signatureVal);
        var timestamp = long.Parse(timestampVal);
        var address = Address.FromPublicKey(publicKey).ToBase58();

        var time = DateTime.UnixEpoch.AddMilliseconds(timestamp);
        var timeRangeConfig = context.HttpContext.RequestServices.GetRequiredService<IOptions<TimeRangeOption>>().Value;
        _logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<SignatureGrantHandler>>();
        _distributedLock = context.HttpContext.RequestServices.GetRequiredService<IAbpDistributedLock>();
        _distributedEventBus = context.HttpContext.RequestServices.GetRequiredService<IDistributedEventBus>();

        if (time < DateTime.UtcNow.AddMinutes(-timeRangeConfig.TimeRange) ||
            time > DateTime.UtcNow.AddMinutes(timeRangeConfig.TimeRange))
        {
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest,
                $"The time should be {timeRangeConfig.TimeRange} minutes before and after the current time.");
        }

        var hash = Encoding.UTF8.GetBytes(address + "-" + timestamp).ComputeHash();
        if (!CryptoHelper.VerifySignature(signature, hash, publicKey))
        {
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "Signature validation failed.");
        }

        var userManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();
        var user = await userManager.FindByNameAsync(address);
        if (user == null)
        {
            var userId = GuidHelper.UniqGuid(address);
            var createUserResult = await CreateUserAsync(userManager, userId, address);
            if (!createUserResult)
            {
                return GetForbidResult(OpenIddictConstants.Errors.ServerError, "Create user failed.");
            }

            user = await userManager.GetByIdAsync(userId);
        }

        var userClaimsPrincipalFactory = context.HttpContext.RequestServices
            .GetRequiredService<IUserClaimsPrincipalFactory<IdentityUser>>();
        var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<IdentityUser>>();
        var principal = await signInManager.CreateUserPrincipalAsync(user);
        var claimsPrincipal = await userClaimsPrincipalFactory.CreateAsync(user);
        claimsPrincipal.SetScopes("EoaServer");
        claimsPrincipal.SetResources(await GetResourcesAsync(context, principal.GetScopes()));
        claimsPrincipal.SetAudiences("EoaServer");

        await context.HttpContext.RequestServices.GetRequiredService<AbpOpenIddictClaimsPrincipalManager>()
            .HandleAsync(context.Request, principal);

        return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
    }

    private ForbidResult CheckParams(string publicKeyVal, string signatureVal, string timestampVal)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(publicKeyVal))
        {
            errors.Add("invalid parameter pubkey.");
        }

        if (string.IsNullOrWhiteSpace(signatureVal))
        {
            errors.Add("invalid parameter signature.");
        }

        if (string.IsNullOrWhiteSpace(timestampVal) || !long.TryParse(timestampVal, out var time) || time <= 0)
        {
            errors.Add("invalid parameter timestamp.");
        }


        if (errors.Count > 0)
        {
            return new ForbidResult(
                new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                properties: new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                        OpenIddictConstants.Errors.InvalidRequest,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = GetErrorMessage(errors)
                }!));
        }

        return null;
    }

    private string GetErrorMessage(List<string> errors)
    {
        var message = string.Empty;

        errors?.ForEach(t => message += $"{t}, ");
        if (message.Contains(','))
        {
            return message.TrimEnd().TrimEnd(',');
        }

        return message;
    }

    private ForbidResult GetForbidResult(string errorType, string errorDescription)
    {
        return new ForbidResult(
            new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
            properties: new AuthenticationProperties(new Dictionary<string, string>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = errorType,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = errorDescription
            }!));
    }

    private async Task<bool> CreateUserAsync(IdentityUserManager userManager, Guid userId, string address)
    {
        var result = false;
        await using var handle =
            await _distributedLock.TryAcquireAsync(name: _lockKeyPrefix + address);
        //get shared lock
        if (handle != null)
        {
            var user = new IdentityUser(userId, userName: address, email: Guid.NewGuid().ToString("N") + "@ABP.IO");
            var identityResult = await userManager.CreateAsync(user);

            if (identityResult.Succeeded)
            {
                await _distributedEventBus.PublishAsync(new UserEto
                {
                    UserId = userId,
                    Address = address
                }, false, false);
                _logger.LogInformation("create user success, userId:{userId}, address:{address}", userId.ToString(),
                    address);
            }

            result = identityResult.Succeeded;
        }
        else
        {
            _logger.LogError("do not get lock, keys already exits, userId:{userId}, address:{address}",
                userId.ToString(), address);
        }

        return result;
    }

    private async Task<IEnumerable<string>> GetResourcesAsync(ExtensionGrantContext context,
        ImmutableArray<string> scopes)
    {
        var resources = new List<string>();
        if (!scopes.Any())
        {
            return resources;
        }

        await foreach (var resource in context.HttpContext.RequestServices.GetRequiredService<IOpenIddictScopeManager>()
                           .ListResourcesAsync(scopes))
        {
            resources.Add(resource);
        }

        return resources;
    }

    public string Name { get; } = "signature";
}