using System;
using System.Threading.Tasks;

namespace NatureBears.Save
{
    /// <summary>
    /// NTP / server-time hook for stronger anti-time-skip validation.
    /// SaveManager falls back to local UtcNow comparison while this returns null.
    /// </summary>
    public interface INetworkTimeService
    {
        /// <summary>Trusted UTC time, or null when unavailable (offline / not implemented).</summary>
        Task<DateTime?> GetUtcNowAsync();
    }

    /// <summary>Inert default until an NTP/server-time source is integrated.</summary>
    public sealed class NullNetworkTimeService : INetworkTimeService
    {
        public Task<DateTime?> GetUtcNowAsync() => Task.FromResult<DateTime?>(null);
    }
}
