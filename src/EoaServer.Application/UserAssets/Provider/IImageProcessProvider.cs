using System.Threading.Tasks;
using JetBrains.Annotations;

namespace EoaServer.UserAssets.Provider;

public interface IImageProcessProvider
{
    Task<string> GetResizeImageAsync(string imageUrl, int width, int height, ImageResizeType type);
}