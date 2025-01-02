using System.Collections.Generic;
using System.Threading.Tasks;
using EoaServer.Options;
using EoaServer.Provider.Dto.Indexer;
using EoaServer.Token;
using EoaServer.Token.Dto;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Options;
using Orleans;
using Serilog;
using Volo.Abp.DependencyInjection;

namespace EoaServer.Provider;

public class GraphQLProvider : IGraphQLProvider, ISingletonDependency
{
    private readonly GraphQLOptions _graphQLOptions;
    private readonly GraphQLHttpClient _blockChainIndexerClient;
    private readonly GraphQLHttpClient _tokenIndexerClient;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger _logger;
    private readonly ITokenAppService _tokenAppService;

    public const string TokenIndexer = "TokenIndexer";
    public const string BlockChainIndexer = "BlockChainIndexer";

    public GraphQLProvider(IClusterClient clusterClient,
        ITokenAppService tokenAppService,
        IOptionsSnapshot<GraphQLOptions> graphQLOptions)
    {
        _logger = Log.ForContext<GraphQLProvider>();
        _clusterClient = clusterClient;
        _graphQLOptions = graphQLOptions.Value;
        _blockChainIndexerClient = new GraphQLHttpClient(_graphQLOptions.IndexerOptions[BlockChainIndexer].BaseUrl, new NewtonsoftJsonSerializer());
        _tokenIndexerClient = new GraphQLHttpClient(_graphQLOptions.IndexerOptions[TokenIndexer].BaseUrl, new NewtonsoftJsonSerializer());
        _tokenAppService = tokenAppService;
    }

    public async Task<IndexerTokenTransferListDto> GetTokenTransferInfoAsync(TokenTransferInput input)
    {
        input.SetDefaultSort();
        var indexerResult = await _tokenIndexerClient.SendQueryAsync<IndexerTokenTransfersDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String!,$symbol:String!,$address:String,$collectionSymbol:String,
                    $search:String,$skipCount:Int!,$maxResultCount:Int!,$types:[SymbolType!],$beginBlockTime:DateTime,
                    $fuzzySearch:String,$sort:String,$orderBy:String,$searchAfter:[String],$orderInfos:[OrderInfo]){
                    transferInfo(input: {chainId:$chainId,symbol:$symbol,collectionSymbol:$collectionSymbol,address:$address,types:$types,beginBlockTime:$beginBlockTime,search:$search,
                    skipCount:$skipCount,maxResultCount:$maxResultCount,fuzzySearch:$fuzzySearch,sort:$sort,orderBy:$orderBy,searchAfter:$searchAfter,orderInfos:$orderInfos}){     
                    totalCount,
                    items{
                        transactionId
                        metadata {
                            chainId
                            block {
                                blockHash
                                blockHeight
                                blockTime
                            }
                        }
                  }                     
                }
            }",
            Variables = new
            {
                chainId = input.ChainId, symbol = input.Symbol, address = input.Address, search = input.Search,
                skipCount = input.SkipCount, maxResultCount = input.MaxResultCount,
                collectionSymbol = input.CollectionSymbol,
                sort = input.Sort, fuzzySearch = input.FuzzySearch,
                orderInfos = input.OrderInfos, searchAfter = input.SearchAfter, beginBlockTime = input.BeginBlockTime
            }
        });
        return indexerResult == null  || indexerResult.Data == null ? 
            new IndexerTokenTransferListDto() : indexerResult.Data.TransferInfo;
    }
}