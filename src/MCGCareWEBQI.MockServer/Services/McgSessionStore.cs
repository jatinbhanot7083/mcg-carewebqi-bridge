using MCGCareWEBQI.MockServer.Models;
using Microsoft.Extensions.Caching.Memory;

namespace MCGCareWEBQI.MockServer.Services;

/// In-memory store of pending mock MCG sessions. Sessions are short-lived
/// (one user, one popup window). Production MCG would persist these server-side
/// for the lifetime of the documentation session.
public sealed class McgSessionStore(IMemoryCache cache)
{
    private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(2);

    public McgMockSession Create(IDictionary<string, string> inboundFields)
    {
        var session = new McgMockSession
        {
            InboundFields = new Dictionary<string, string>(inboundFields, StringComparer.Ordinal)
        };
        cache.Set(KeyFor(session.SessionId), session, SessionTtl);
        return session;
    }

    public McgMockSession? Get(Guid sessionId) =>
        cache.TryGetValue<McgMockSession>(KeyFor(sessionId), out var session) ? session : null;

    public void Save(McgMockSession session) => cache.Set(KeyFor(session.SessionId), session, SessionTtl);

    public void Remove(Guid sessionId) => cache.Remove(KeyFor(sessionId));

    private static string KeyFor(Guid id) => $"mock-mcg:session:{id}";
}
