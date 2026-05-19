using System.Collections.Concurrent;
using MCGCareWEBQI.MockServer.Models;
using Microsoft.Extensions.Caching.Memory;

namespace MCGCareWEBQI.MockServer.Services;

/// In-memory store of pending mock MCG sessions. Sessions are short-lived
/// (one user, one popup window). Production MCG would persist these server-side
/// for the lifetime of the documentation session.
///
/// Also tracks episodeID → most-recent-session mapping so re-launches can detect
/// an existing episode (cert-required for episode re-launch + patient merge scenarios).
public sealed class McgSessionStore(IMemoryCache cache)
{
    private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(2);

    // episodeId -> sessionId of the most recent session that owned this episode.
    private static readonly ConcurrentDictionary<string, Guid> _episodeIndex = new(StringComparer.Ordinal);

    public McgMockSession Create(IDictionary<string, string> inboundFields)
    {
        var session = new McgMockSession
        {
            InboundFields = new Dictionary<string, string>(inboundFields, StringComparer.Ordinal)
        };
        cache.Set(KeyFor(session.SessionId), session, SessionTtl);
        if (inboundFields.TryGetValue("episodeID", out var ep) && !string.IsNullOrEmpty(ep))
        {
            _episodeIndex[ep] = session.SessionId;
        }
        return session;
    }

    public McgMockSession? Get(Guid sessionId) =>
        cache.TryGetValue<McgMockSession>(KeyFor(sessionId), out var session) ? session : null;

    public void Save(McgMockSession session)
    {
        cache.Set(KeyFor(session.SessionId), session, SessionTtl);
        if (!string.IsNullOrEmpty(session.EpisodeId))
        {
            _episodeIndex[session.EpisodeId] = session.SessionId;
        }
    }

    public void Remove(Guid sessionId) => cache.Remove(KeyFor(sessionId));

    /// Find the most recent session that handled the given episodeID. Used during re-launch
    /// to detect (a) the same episode being re-opened, (b) a patient ID change requiring
    /// the merge prompt. Returns null if no such session is still alive in the cache.
    public McgMockSession? FindByEpisode(string episodeId)
    {
        if (string.IsNullOrEmpty(episodeId)) return null;
        return _episodeIndex.TryGetValue(episodeId, out var sid) ? Get(sid) : null;
    }

    private static string KeyFor(Guid id) => $"mock-mcg:session:{id}";
}
