using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using EoaServer.Commons;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace EoaServer.UserAssets.Provider;

public interface IImageProcessProvider
{
    Task<string> GetResizeImageAsync(string imageUrl, int width, int height, ImageResizeType type);
}

public class ImageProcessProvider : IImageProcessProvider, ISingletonDependency
{
    private readonly ILogger<ImageProcessProvider> _logger;
    private readonly AwsThumbnailOptions _awsThumbnailOptions;


    private HttpClient Client { get; set; }

    public ImageProcessProvider(ILogger<ImageProcessProvider> logger,
        IOptions<AwsThumbnailOptions> awsThumbnailOptions)
    {
        _logger = logger;
        _awsThumbnailOptions = awsThumbnailOptions.Value;
    }

    public async Task<string> GetResizeImageAsync(string imageUrl, int width, int height, ImageResizeType type)
    {
        try
        {
            if (!imageUrl.StartsWith(CommonConstant.ProtocolName))
            {
                return imageUrl;
            }

            if (!_awsThumbnailOptions.ExcludedSuffixes.Contains(GetImageUrlSuffix(imageUrl)))
            {
                return imageUrl;
            }

            var bucket = imageUrl.Split("/")[2];
            if (!_awsThumbnailOptions.BucketList.Contains(bucket))
            {
                return imageUrl;
            }

            var resizeWidth = Enum.GetValues(typeof(ImageResizeWidthType)).Cast<ImageResizeWidthType>()
                .FirstOrDefault(a => (int)a == width);

            var reizeHeight = Enum.GetValues(typeof(ImageResizeHeightType)).Cast<ImageResizeHeightType>()
                .FirstOrDefault(a => (int)a == height);

            if (resizeWidth == ImageResizeWidthType.None || reizeHeight == ImageResizeHeightType.None)
            {
                return imageUrl;
            }

            return await GetResizeImageUrlAsync(imageUrl, width, height);
        }
        catch (Exception ex)
        {
            _logger.LogError("sendImageRequest Execption:", ex);
            return imageUrl;
        }
    }

    public string GetResizeUrl(string imageUrl, int width, int height, bool replaceDomain, ImageResizeType type)
    {
        if (replaceDomain)
        {
            var urlSplit = imageUrl.Split(new string[] { UserAssetsServiceConstant.AwsDomain },
                StringSplitOptions.RemoveEmptyEntries);
            imageUrl = type switch
            {
                ImageResizeType.PortKey => _awsThumbnailOptions.PortKeyBaseUrl + urlSplit[1],
                ImageResizeType.Im => _awsThumbnailOptions.ImBaseUrl + urlSplit[1],
                ImageResizeType.Forest => _awsThumbnailOptions.ForestBaseUrl + urlSplit[1],
                _ => imageUrl
            };
        }

        var lastIndexOf = imageUrl.LastIndexOf("/", StringComparison.Ordinal);
        var pre = imageUrl.Substring(0, lastIndexOf);
        var last = imageUrl.Substring(lastIndexOf, imageUrl.Length - lastIndexOf);
        var resizeImage = pre + "/" + (width == -1 ? "AUTO" : width) + "x" + (height == -1 ? "AUTO" : height) + last;
        return resizeImage;
    }

    private async Task SendUrlAsync(string url, string? version = null)
    {
        Client ??= new HttpClient();
        Client.DefaultRequestHeaders.Accept.Clear();
        Client.DefaultRequestHeaders.Accept.Add(
            MediaTypeWithQualityHeaderValue.Parse($"application/json{version}"));
        Client.DefaultRequestHeaders.Add("Connection", "close");
        await Client.GetAsync(url);
    }

    private async Task<string> GetResizeImageUrlAsync(string imageUrl, int width, int height)
    {
        var type = GetS3Type(imageUrl);
        var produceImage = GetResizeUrl(imageUrl, width, height, true, type);
        await SendUrlAsync(produceImage);

        var resImage = GetResizeUrl(imageUrl, width, height, false, type);
        return resImage;
    }

    private ImageResizeType GetS3Type(string imageUrl)
    {
        var urlSplit = imageUrl.Split(new string[] { UserAssetsServiceConstant.AwsDomain },
            StringSplitOptions.RemoveEmptyEntries);

        if (urlSplit[0].ToLower().Contains(CommonConstant.ImS3Mark))
        {
            return ImageResizeType.Im;
        }

        if (urlSplit[0].ToLower().Contains(CommonConstant.PortkeyS3Mark))
        {
            return ImageResizeType.PortKey;
        }

        return ImageResizeType.Forest;
    }

    private string GetImageUrlSuffix(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        var imageUrlArray = imageUrl.Split(".");
        return imageUrlArray[^1].ToLower();
    }
}