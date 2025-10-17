using System.Collections.Generic;

namespace EoaServer.Commons;

public class ListResponseDto<T>
{
    public long Total { get; set; }
    public List<T> List { get; set; } = new();
}