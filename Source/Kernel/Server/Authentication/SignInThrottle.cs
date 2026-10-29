// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Tracks a bounded set of failed sign-ins on this node.
/// </summary>
/// <param name="options">The Chronicle options.</param>
/// <param name="timeProvider">The clock.</param>
public class SignInThrottle(IOptions<ChronicleOptions> options, TimeProvider timeProvider) : ISignInThrottle
{
    readonly SignInThrottling _settings = options.Value.Authentication.SignInThrottling;
    readonly ConcurrentDictionary<(string Username, string Address), Entry> _entries = new();
    readonly Lock _lock = new();

    /// <summary>
    /// Gets the number of currently tracked pairs.
    /// </summary>
    internal int TrackedEntries => _entries.Count;

    /// <inheritdoc/>
    public bool IsLockedOut(string username, string address)
    {
        if (_settings.MaxFailedAttempts <= 0) return false;
        lock (_lock)
        {
            var now = timeProvider.GetUtcNow();
            Prune(now);
            if (_entries.TryGetValue(Key(username, address), out var entry)) return entry.LockedUntil > now;

            // Keep active entries rather than evicting another caller's lockout when capacity is reached.
            return _entries.Count >= Math.Max(1, _settings.MaxTrackedEntries);
        }
    }

    /// <inheritdoc/>
    public void Failed(string username, string address)
    {
        if (_settings.MaxFailedAttempts <= 0) return;
        lock (_lock)
        {
            var now = timeProvider.GetUtcNow();
            Prune(now);
            var key = Key(username, address);
            if (!_entries.TryGetValue(key, out var entry))
            {
                if (_entries.Count >= Math.Max(1, _settings.MaxTrackedEntries)) return;
                entry = new(0, now.AddSeconds(Math.Max(1, _settings.WindowSeconds)), DateTimeOffset.MinValue);
            }
            if (entry.LockedUntil > now) return;
            var failures = entry.Failures + 1;
            _entries[key] = entry with
            {
                Failures = failures,
                LockedUntil = failures >= _settings.MaxFailedAttempts
                    ? now.AddSeconds(Math.Max(1, _settings.LockoutSeconds))
                    : DateTimeOffset.MinValue
            };
        }
    }

    /// <inheritdoc/>
    public void Succeeded(string username, string address)
    {
        lock (_lock) _entries.TryRemove(Key(username, address), out _);
    }

    static (string Username, string Address) Key(string username, string address) => (username.ToUpperInvariant(), address);

    void Prune(DateTimeOffset now)
    {
        foreach (var (key, entry) in _entries)
        {
            var expires = entry.LockedUntil == DateTimeOffset.MinValue ? entry.WindowEnd : entry.LockedUntil;
            if (expires <= now) _entries.TryRemove(key, out _);
        }
    }

    sealed record Entry(int Failures, DateTimeOffset WindowEnd, DateTimeOffset LockedUntil);
}
