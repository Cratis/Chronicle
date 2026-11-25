// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// Counts the commands that reach the database, per command type. Counting happens at the driver (MongoDB command
/// monitoring) or at EF Core (a command interceptor), never by mocking, so the counts are the real round trips.
/// </summary>
public sealed class CommandCounter
{
    /// <summary>Handshake and session housekeeping commands that are not storage work.</summary>
    static readonly HashSet<string> _ignored = new(StringComparer.OrdinalIgnoreCase)
    {
        "hello", "ismaster", "ping", "saslstart", "saslcontinue", "buildinfo", "endsessions", "getnonce", "logout", "authenticate"
    };

    readonly ConcurrentDictionary<string, long> _byType = new(StringComparer.Ordinal);
    long _total;

    /// <summary>Gets whether a command name is housekeeping that is not counted.</summary>
    /// <param name="commandName">The command name.</param>
    /// <returns><see langword="true"/> when the command is ignored.</returns>
    public static bool IsIgnored(string commandName) => _ignored.Contains(commandName);

    /// <summary>Records one command.</summary>
    /// <param name="commandType">The command type.</param>
    public void Record(string commandType)
    {
        if (IsIgnored(commandType)) return;
        _byType.AddOrUpdate(commandType, 1, static (_, count) => count + 1);
        Interlocked.Increment(ref _total);
    }

    /// <summary>Takes a snapshot of what has been counted.</summary>
    /// <returns>The snapshot.</returns>
    public CommandSnapshot Snapshot() => new(Interlocked.Read(ref _total), new Dictionary<string, long>(_byType));

    /// <summary>Clears all counts.</summary>
    public void Reset()
    {
        _byType.Clear();
        Interlocked.Exchange(ref _total, 0);
    }
}
