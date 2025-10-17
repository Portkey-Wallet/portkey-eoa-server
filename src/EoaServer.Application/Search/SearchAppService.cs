using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using EoaServer.Entities.Es;
using EoaServer.Search.Dto;
using Nest;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;

namespace EoaServer.Search;

[RemoteService(false)]
[DisableAuditing]
public class SearchAppService : EoaServerBaseService, ISearchAppService
{
    private readonly INESTRepository<ChainsInfoIndex, string> _chainsInfoRepository;
    private readonly IObjectMapper _objectMapper;

    public SearchAppService(
        INESTRepository<ChainsInfoIndex, string> chainsInfoRepository,
        IObjectMapper objectMapper)
    {
        _chainsInfoRepository = chainsInfoRepository;
        _objectMapper = objectMapper;
    }

    public async Task<PagedResultDto<ChainsInfoDto>> GetChainsInfoAsync()
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ChainsInfoIndex>, QueryContainer>>();
        QueryContainer Filter(QueryContainerDescriptor<ChainsInfoIndex> f) => f.Bool(b => b.Must(mustQuery));

        var (totalCount, list) = await _chainsInfoRepository.GetListAsync(Filter);

        return new PagedResultDto<ChainsInfoDto>
        {
            TotalCount = totalCount,
            Items = _objectMapper.Map<List<ChainsInfoIndex>, List<ChainsInfoDto>>(list)
        };
    }
}