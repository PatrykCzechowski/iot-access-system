using System.Collections.Concurrent;

namespace AccessControl.Infrastructure.Mqtt;

public sealed class HeartbeatThrottler
{
    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(30);
    private const int MaxCacheSize = 500;
    private readonly ConcurrentDictionary<Guid, DateTime> _lastPersisted = new();

    public bool ShouldPersist(Guid hardwareId)
    {
        var now = DateTime.UtcNow;
        var shouldPersist = false;

        _lastPersisted.AddOrUpdate(
            hardwareId,
            _ => { shouldPersist = true; return now; },
            (_, last) =>
            {
                if (now - last >= MinInterval)
                {
                    shouldPersist = true;
                    return now;
                }
                return last;
            });

        if (shouldPersist)
        {
            EvictStaleEntries(now);
        }

        return shouldPersist;
    }

    private void EvictStaleEntries(DateTime now)
    {
        if (_lastPersisted.Count <= MaxCacheSize)
        {
            return;
        }

        var stale = _lastPersisted
            .Where(kv => now - kv.Value > TimeSpan.FromMinutes(10))
            .Select(kv => kv.Key)
            .ToList();

        foreach (var key in stale)
        {
            _lastPersisted.TryRemove(key, out _);
        }
    }
}
