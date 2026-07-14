using System.Threading.Tasks;

namespace NatureBears.Save
{
    /// <summary>
    /// Cloud half of the hybrid save system. JSON travels PLAIN through this
    /// interface (transport security is the provider's job) and is re-encrypted
    /// locally with the current device's key — this is the recovery path when
    /// the device-bound AES key changes (e.g. iOS reinstall).
    /// Concrete implementation (Unity Cloud Save / Play Games / GameCenter) TBD.
    /// </summary>
    public interface ICloudSaveProvider
    {
        bool IsAvailable { get; }
        Task<string> DownloadJsonAsync();
        Task UploadJsonAsync(string json);
    }

    /// <summary>Inert default until a cloud backend is integrated.</summary>
    public sealed class NullCloudSaveProvider : ICloudSaveProvider
    {
        public bool IsAvailable => false;
        public Task<string> DownloadJsonAsync() => Task.FromResult<string>(null);
        public Task UploadJsonAsync(string json) => Task.CompletedTask;
    }
}
