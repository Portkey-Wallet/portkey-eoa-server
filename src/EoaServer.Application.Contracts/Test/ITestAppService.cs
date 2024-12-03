using System.Threading.Tasks;

namespace EoaServer.Test;

public interface ITestAppService
{
    Task<string> TestAsync();
}