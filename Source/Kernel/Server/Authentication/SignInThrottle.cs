// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Tracks bounded username and username/address failure counters on this node.
/// </summary>
/// <param name="options">The Chronicle options.</param>
/// <param name="timeProvider">The clock.</param>
public class SignInThrottle(IOptions<ChronicleOptions> options, TimeProvider timeProvider) : ISignInThrottle
{
    readonly SignInThrottling _settings = options.Value.Authentication.SignInThrottling;
    readonly Dictionary<(string Username, string Address), Entry> _users = new();
    readonly Dictionary<(string Username, string Address), Entry> _addresses = new();
    readonly Lock _lock = new();

    /// <summary>
    /// Gets the number of currently tracked counters.
    /// </summary>
    internal int TrackedEntries
    {
        get
        {
            lock (_lock) return _users.Count + _addresses.Count;
        }
    }

    /// <inheritdoc/>
    public bool IsLockedOut(string username, string address)
    {
        if (_settings.MaxFailedAttempts <= 0) return false;
        lock (_lock)
        {
            var now = timeProvider.GetUtcNow();
            Prune(_users, now);
            Prune(_addresses, now);
            return Locked(_users, Normalize((username, string.Empty)), now) || Locked(_addresses, Normalize((username, address)), now);
        }
    }

    /// <inheritdoc/>
    public void Failed(string username, string address)
    {
        if (_settings.MaxFailedAttempts <= 0) return;
        lock (_lock)
        {
            var now = timeProvider.GetUtcNow();
            var addressKey = Normalize((username, address));
            if (Locked(_addresses, addressKey, now)) return;
            Record(_users, Normalize((username, string.Empty)), now, Math.Max((long)_settings.MaxFailedAttempts + 1, _settings.MaxFailedAttemptsPerUsername));
            Record(_addresses, addressKey, now, _settings.MaxFailedAttempts);
        }
    }

    /// <inheritdoc/>
    public void Succeeded(string username, string address)
    {
        lock (_lock)
        {
            _users.Remove(Normalize((username, string.Empty)));
            _addresses.Remove(Normalize((username, address)));
        }
    }

    static (string Username, string Address) Normalize((string Username, string Address) key) => (key.Username.ToUpperInvariant(), Bucket(key.Address));

    static string Bucket(string address)
    {
        if (!IPAddress.TryParse(address, out var ip)) return address;
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        var bytes = ip.GetAddressBytes();
        if (bytes.Length == 16) Array.Clear(bytes, 8, 8);
        return new IPAddress(bytes).ToString();
    }

    static bool Locked(Dictionary<(string Username, string Address), Entry> entries, (string Username, string Address) key, DateTimeOffset now) =>
        entries.TryGetValue(key, out var entry) && entry.LockedUntil > now;

    static void Prune(Dictionary<(string Username, string Address), Entry> entries, DateTimeOffset now)
    {
        foreach (var key in entries.Where(pair => pair.Value.Expires <= now).Select(pair => pair.Key).ToArray()) entries.Remove(key);
    }

    void Record(Dictionary<(string Username, string Address), Entry> entries, (string Username, string Address) key, DateTimeOffset now, long maxFailedAttempts)
    {
        Prune(entries, now);
        if (!entries.TryGetValue(key, out var entry))
        {
            if (entries.Count >= Math.Max(1, _settings.MaxTrackedEntries))
            {
                // Prefer an unlocked counter, then the oldest counter. Admission never locks out an untracked caller.
                var oldest = entries.OrderBy(pair => pair.Value.LockedUntil > now).ThenBy(pair => pair.Value.Created).First().Key;
                entries.Remove(oldest);
            }
            entry = new(0, now, now.AddSeconds(Math.Max(1, _settings.WindowSeconds)), DateTimeOffset.MinValue);
        }
        if (entry.LockedUntil > now) return;
        var failures = entry.Failures + 1;
        entries[key] = entry with
        {
            Failures = failures,
            LockedUntil = failures >= maxFailedAttempts
                ? now.AddSeconds(Math.Max(1, _settings.LockoutSeconds))
                : DateTimeOffset.MinValue
        };
    }

    sealed record Entry(int Failures, DateTimeOffset Created, DateTimeOffset WindowEnd, DateTimeOffset LockedUntil)
    {
        public DateTimeOffset Expires => LockedUntil == DateTimeOffset.MinValue ? WindowEnd : LockedUntil;
    }
}
