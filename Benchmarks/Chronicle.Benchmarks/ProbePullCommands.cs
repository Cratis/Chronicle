// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MongoDB.Bson;
using MongoDB.Driver.Core.Events;

namespace Cratis.Chronicle.Benchmarks;

// system.profile emits an update row per statement even when one wire command contains 1000 updates.
// Keep the inherited profiler totals intact; expose actual coordinator wire commands separately.
internal sealed class ProbePullCommands
{
    readonly ConcurrentQueue<(DateTime At, string Name, int Documents)> _commands = new();
    DateTime _start;
    DateTime _end;

    internal void Record(CommandStartedEvent evt)
    {
        var command = evt.Command;
        var ownComment = command.GetValue("comment", BsonNull.Value) == ProbePullMongoSource.Comment;
        var ownGetMore = evt.CommandName == "getMore" && command.GetValue("collection", "") == "event-log";
        if (!ownComment && !ownGetMore) return;
        var documents = evt.CommandName == "update" ? command["updates"].AsBsonArray.Count : 0;
        _commands.Enqueue((DateTime.UtcNow, evt.CommandName, documents));
    }

    internal void Start() => _start = DateTime.UtcNow;
    internal void End() => _end = DateTime.UtcNow;
    internal object Snapshot()
    {
        var rows = _commands.Where(c => c.At >= _start && c.At <= _end).ToArray();
        return new
        {
            method = "CommandStartedEvent on dedicated pull client; exact clock window; attempts counted, including retries; profiler update rows remain per-statement",
            startUtc = _start, endUtc = _end,
            rows = rows.GroupBy(c => c.Name).Select(g => new { command = g.Key, commands = g.Count(), documentsWritten = g.Sum(c => c.Documents) }).ToArray(),
            commands = rows.Length,
            reads = rows.Count(c => c.Name is "find" or "getMore"),
            writes = rows.Count(c => c.Name == "update"),
            documentsWritten = rows.Sum(c => c.Documents)
        };
    }
}
