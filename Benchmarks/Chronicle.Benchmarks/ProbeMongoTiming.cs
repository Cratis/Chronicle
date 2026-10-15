// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Benchmarks;

internal sealed partial class ProbeMongo
{
    readonly ConcurrentQueue<BsonDocument> _timingWrites = new();
    IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>? _timingCursor;
    CancellationTokenSource? _timingStop;
    Task? _timingPump;

    // Timing-only catch-up still requires a successful job replacement followed by that same job's deletion.
    // Replace events carry the exact full document in the oplog; updateLookup would race deletion and is not accepted.
    internal async Task StartTimingEvidence(CancellationToken cancellation)
    {
        _timingStop = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        _timingCursor = await PullDatabase.GetCollection<BsonDocument>("jobs").WatchAsync(
            new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>(),
            new ChangeStreamOptions { MaxAwaitTime = TimeSpan.FromMilliseconds(100) }, _timingStop.Token);
        _timingPump = Task.Run(async () =>
        {
            while (await _timingCursor.MoveNextAsync(_timingStop.Token))
            {
                foreach (var change in _timingCursor.Current)
                {
                    var replace = change.OperationType == ChangeStreamOperationType.Replace;
                    if (!replace && change.OperationType != ChangeStreamOperationType.Delete) continue;
                    var command = new BsonDocument("q", change.DocumentKey);
                    if (replace) command["u"] = change.FullDocument ?? throw new InvalidOperationException("Job replace lacks fullDocument.");
                    // Cluster time plus local arrival order preserves replacement-before-delete; same-second is valid.
                    _timingWrites.Enqueue(new BsonDocument
                    {
                        { "op", replace ? "update" : "remove" }, { "command", command },
                        { "ts", new BsonDateTime(DateTimeOffset.FromUnixTimeSeconds(change.ClusterTime.Timestamp).UtcDateTime) },
                        { "ndeleted", replace ? 0 : 1 }
                    });
                }
            }
        });
    }

    BsonDocument[] TimingJobWrites() => _timingWrites.ToArray()
        .GroupBy(d => (d["command"]["q"]["_id"].ToJson(), d["op"].AsString)).Select(g => g.Last()).ToArray();

    internal async Task StopTimingEvidence()
    {
        if (_timingStop is null) return;
        _timingStop.Cancel();
        try { if (_timingPump is not null) await _timingPump; }
        catch (OperationCanceledException) when (_timingStop.IsCancellationRequested) { }
        finally { _timingCursor?.Dispose(); _timingStop.Dispose(); }
    }

    internal async Task<JsonElement> CpuSnapshot()
    {
        // Read-only /proc snapshots inside this task's container, not docker's host-wide CPU percentage.
        var result = await container.ExecAsync(["sh", "-c", "printf 'CGROUP\\n'; cat /sys/fs/cgroup/cpu.stat; printf 'HZ '; getconf CLK_TCK; for p in /proc/[0-9]*; do [ -r \"$p/stat\" ] || continue; IFS= read -r line < \"$p/stat\" || continue; case \"$line\" in *'(mongod)'*|*'(dotnet)'*|*'(Cratis.Chronicl)'*) printf '%s\\n' \"$line\";; esac; done"]);
        if (result.ExitCode != 0) throw new InvalidOperationException($"Container CPU snapshot failed: {result.Stderr}");
        var lines = result.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var hz = long.Parse(lines.Single(l => l.StartsWith("HZ ", StringComparison.Ordinal)).Split(' ')[1]);
        var usage = long.Parse(lines.Single(l => l.StartsWith("usage_usec ", StringComparison.Ordinal)).Split(' ')[1]);
        var processes = lines.Where(l => l.Contains('(')).Select(l =>
        {
            var end = l.LastIndexOf(')');
            var fields = l[(end + 2)..].Split(' ');
            return new { pid = int.Parse(l[..l.IndexOf(' ')]), name = l[(l.IndexOf('(') + 1)..end],
                userTicks = long.Parse(fields[11]), systemTicks = long.Parse(fields[12]) };
        }).ToArray();
        if (!processes.Any(p => p.name == "mongod") || !processes.Any(p => p.name != "mongod"))
            throw new InvalidOperationException("CPU snapshot must identify both server and mongod.");
        return JsonSerializer.SerializeToElement(new { label = "Chronicle kernel + mongod combined", usageUsec = usage, hz, processes });
    }
}
