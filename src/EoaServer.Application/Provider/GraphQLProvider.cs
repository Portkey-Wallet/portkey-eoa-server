using System.Linq;
using System.Threading.Tasks;
using EoaServer.Options;
using EoaServer.Provider.Dto.Indexer;
using EoaServer.Token;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Options;
using Serilog;
using Volo.Abp.DependencyInjection;

namespace EoaServer.Provider;

public class GraphQLProvider : IGraphQLProvider, ISingletonDependency
{
    private readonly GraphQLOptions _graphQLOptions;
    private readonly GraphQLHttpClient _blockChainIndexerClient;
    private readonly GraphQLHttpClient _tokenIndexerClient;
    private readonly ILogger _logger;

    public const string TokenIndexer = "TokenIndexer";
    public const string BlockChainIndexer = "BlockChainIndexer";

    public GraphQLProvider(
        IOptionsSnapshot<GraphQLOptions> graphQLOptions)
    {
        _logger = Log.ForContext<GraphQLProvider>();
        _graphQLOptions = graphQLOptions.Value;
        _blockChainIndexerClient = new GraphQLHttpClient(_graphQLOptions.IndexerOptions[BlockChainIndexer].BaseUrl, new NewtonsoftJsonSerializer());
        _tokenIndexerClient = new GraphQLHttpClient(_graphQLOptions.IndexerOptions[TokenIndexer].BaseUrl, new NewtonsoftJsonSerializer());
    }

    public async Task<IndexerTokenTransferListDto> GetTokenTransferInfoAsync(GetTokenTransferRequestDto requestDto)
    {
        requestDto.SetDefaultSort();
        var graphQlResponse = await _tokenIndexerClient.SendQueryAsync<IndexerTokenTransfersDto>(new GraphQLRequest
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
                chainId = requestDto.ChainId, symbol = requestDto.Symbol, address = requestDto.Address, search = requestDto.Search,
                skipCount = requestDto.SkipCount, maxResultCount = requestDto.MaxResultCount,
                collectionSymbol = requestDto.CollectionSymbol,
                sort = requestDto.Sort, fuzzySearch = requestDto.FuzzySearch,
                orderInfos = requestDto.OrderInfos, searchAfter = requestDto.SearchAfter, beginBlockTime = requestDto.BeginBlockTime
            }
        });
        
        if (graphQlResponse.Errors != null)
        {
            ErrorLog(graphQlResponse.Errors);
            return new IndexerTokenTransferListDto();
        }
        
        return graphQlResponse.Data.TransferInfo;
    }
    
    public async Task<IndexerTransactionListResultDto> GetTransactionsAsync(TransactionsRequestDto requestDto)
    {
        requestDto.SetDefaultSort();
        var graphQlResponse = await _blockChainIndexerClient.SendQueryAsync<IndexerTransactionResultDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$skipCount:Int!,$maxResultCount:Int!,$startTime:Long!,$endTime:Long!,$address:String!,$searchAfter:[String],$orderInfos:[OrderInfo]){
                    transactionInfos(input: {chainId:$chainId,skipCount:$skipCount,maxResultCount:$maxResultCount,startTime:$startTime,endTime:$endTime,address:$address,searchAfter:$searchAfter,orderInfos:$orderInfos})
                {
                  totalCount
                    items {
                       transactionId
                          blockHeight
                          chainId
                          methodName
                          status
                          from
                          to
                          transactionValue
                          fee
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
                chainId = requestDto.ChainId, skipCount = requestDto.SkipCount, maxResultCount = requestDto.MaxResultCount,
                startTime = requestDto.StartTime,
                endTime = requestDto.EndTime, address = requestDto.Address,
                orderInfos = requestDto.OrderInfos, searchAfter = requestDto.SearchAfter
            }
        });
        
        if (graphQlResponse.Errors != null)
        {
            ErrorLog(graphQlResponse.Errors);
            return new IndexerTransactionListResultDto();
        }
        
        return graphQlResponse.Data.TransactionInfos;
    }
    
    private void ErrorLog(GraphQLError[] errors)
    {
        errors.ToList().ForEach(error =>
        {
            _logger.Error("GraphQL error: {message}", error.Message);
        });
    }
}