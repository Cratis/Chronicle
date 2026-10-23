// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Builds and checks a deterministic, separately sourced child hierarchy.
/// </summary>
/// <param name="fixture">The running single-silo kernel.</param>
/// <param name="settings">The workload dimensions.</param>
internal sealed class ReplayShapeDataset(ClusterBenchmarkFixture fixture, ReplayShapeSettings settings)
{
    /// <summary>
    /// The fixed shuffle seed.
    /// </summary>
    internal const int RandomSeed = 5261;
    readonly Dictionary<string, string> _names = new(StringComparer.Ordinal);
    readonly HashSet<string> _removed = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets counts of actual consecutive same-source runs across the entire workload.
    /// </summary>
    internal IReadOnlyDictionary<int, int> RunLengthHistogram { get; private set; } = new Dictionary<int, int>();

    /// <summary>
    /// Gets the model-bound projection handler.
    /// </summary>
    internal IProjectionHandler Handler => fixture.EventStore1.Projections.GetAllHandlers().Single(handler =>
        handler.Id == fixture.EventStore1.Projections.GetProjectionIdForModel<ReplayShapeReadModel>());

    /// <summary>
    /// Appends the exact synthetic corpus in acknowledged batches.
    /// </summary>
    /// <param name="deadline">The scenario cancellation token.</param>
    /// <returns>A task representing seeding and catch-up.</returns>
    /// <exception cref="InvalidOperationException">The seed has the wrong count or an append was refused.</exception>
    internal async Task Seed(CancellationToken deadline)
    {
        var events = BuildEvents().ToArray();
        if (events.Length != settings.Events) throw new InvalidOperationException("The generated event count is incorrect.");
        RunLengthHistogram = ReplayShapeRunHistogram.Count(events.Select(entry => entry.EventSourceId.Value));
        var appended = 0;
        foreach (var batch in events.Chunk(500))
        {
            var result = await fixture.EventStore1.EventLog.AppendMany(batch).WaitAsync(deadline);
            if (!result.IsSuccess) throw new InvalidOperationException("A seed batch was not acknowledged successfully.");
            appended += batch.Length;
            await Console.Error.WriteLineAsync($"{DateTimeOffset.UtcNow:O} Seed acknowledged {appended}/{settings.Events} events.");
        }
        await CaughtUp(deadline);
    }

    /// <summary>
    /// Waits for an active observer at the tail and no jobs in flight.
    /// </summary>
    /// <param name="deadline">The scenario cancellation token.</param>
    /// <returns>A task representing the quiescence check.</returns>
    /// <exception cref="InvalidOperationException">The projection has failed partitions.</exception>
    internal async Task CaughtUp(CancellationToken deadline)
    {
        var timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        await Handler.WaitTillActive(timeout).WaitAsync(deadline);
        var tail = await fixture.EventStore1.EventLog.GetTailSequenceNumber();
        var caughtUp = Handler.WaitTillReachesEventSequenceNumber(tail, timeout);
        while (!caughtUp.IsCompleted)
        {
            if (await Task.WhenAny(caughtUp, Task.Delay(TimeSpan.FromSeconds(10), deadline)) == caughtUp) break;
            var state = await Handler.GetState().WaitAsync(deadline);
            await Console.Error.WriteLineAsync($"{DateTimeOffset.UtcNow:O} Observer {state.RunningState}, last handled {state.LastHandledEventSequenceNumber}, target {tail}.");
            if ((await Handler.GetFailedPartitions()).Any()) throw new InvalidOperationException("The shape projection has failed partitions.");
        }
        await caughtUp.WaitAsync(deadline);
        await Console.Error.WriteLineAsync($"{DateTimeOffset.UtcNow:O} Observer reached tail {tail}; waiting for jobs.");
        await fixture.Readiness.WaitForNoJobsInFlight(timeout).WaitAsync(deadline);
        if ((await Handler.GetFailedPartitions()).Any()) throw new InvalidOperationException("The shape projection has failed partitions.");
    }

    /// <summary>
    /// Checks every root and child against the generator and hashes canonical content.
    /// </summary>
    /// <param name="deadline">The scenario cancellation token.</param>
    /// <returns>The verified document count and content hash.</returns>
    /// <exception cref="InvalidOperationException">The projected documents do not match the generated workload.</exception>
    internal async Task<(int Count, string Hash)> Snapshot(CancellationToken deadline)
    {
        var models = (await fixture.EventStore1.ReadModels.Materialized.GetInstances<ReplayShapeReadModel>(take: settings.Roots + 1)
            .WaitAsync(deadline)).OrderBy(model => model.Id, StringComparer.Ordinal).ToArray();
        if (models.Length != settings.Roots) throw new InvalidOperationException($"Expected {settings.Roots} roots, got {models.Length}.");
        foreach (var model in models)
        {
            if (model.Name != model.Id) throw new InvalidOperationException($"Incorrect root name: {model.Id}.");
            VerifyChildren(model.Children, model.Id);
        }
        var canonical = models.Select(model => new { model.Id, model.Name, Children = CanonicalChildren(model.Children) });
        return (models.Length, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(canonical)))));
    }

    static string ChildId(string root, int level) => $"{root}-child-{level}";

    static object[] CanonicalChildren(IEnumerable<ReplayShapeChild>? children) => (children ?? [])
        .OrderBy(child => child.Id, StringComparer.Ordinal)
        .Select(child => (object)new { child.Id, child.Name, Children = (child.Leaves ?? []).OrderBy(leaf => leaf.Id, StringComparer.Ordinal).Select(leaf => new { leaf.Id, leaf.Name }).ToArray() }).ToArray();

    IEnumerable<EventForEventSourceId> BuildEvents()
    {
        var random = new Random(RandomSeed);
        var roots = Enumerable.Range(0, settings.Roots).Select(index => $"root-{index:D6}").ToArray();
        var leaves = roots.Select(root => ChildId(root, settings.Depth)).OrderBy(_ => random.Next()).ToArray();
        var removeCount = (int)Math.Ceiling(settings.Roots * settings.RemovePercent / 100d);
        _removed.UnionWith(leaves.Take(removeCount));
        var nodeCount = checked(settings.Roots * settings.Depth);
        var updates = settings.Events - settings.Roots - nodeCount - removeCount;

        foreach (var root in roots.OrderBy(_ => random.Next())) yield return new(root, new ReplayShapeRootCreated(root));
        for (var level = 1; level <= settings.Depth; level++)
        {
            foreach (var root in roots.OrderBy(_ => random.Next()))
            {
                var id = ChildId(root, level);
                _names.Add(id, id);
                object added = level == 1
                    ? new ReplayShapeChildAdded(root, id, id)
                    : new ReplayShapeDescendantAdded(ChildId(root, level - 1), id, id);
                yield return new(id, added);
            }
        }

        var nodes = roots.SelectMany(root => Enumerable.Range(1, settings.Depth).Select(level => (Level: level, Id: ChildId(root, level), Parent: level == 1 ? root : ChildId(root, level - 1))))
            .OrderBy(_ => random.Next()).ToArray();
        var remaining = nodes.Select((_, index) => (updates / nodeCount) + (index < updates % nodeCount ? 1 : 0)).ToArray();
        var revision = 0;
        var firstRound = true;
        while (revision < updates)
        {
            for (var index = 0; index < nodes.Length && revision < updates; index++)
            {
                if (settings.RunLengthMix is null && remaining[index] == 0) continue;

                // Mixed runs share a global budget; reserve one rename for each not-yet-visited child.
                var available = settings.RunLengthMix is null ? remaining[index] : updates - revision - (firstRound ? nodes.Length - index - 1 : 0);
                var run = Math.Min(settings.RunLengthMix?.Sample(random) ?? settings.RunLength, available);
                for (var entry = 0; entry < run; entry++)
                {
                    var node = nodes[index];
                    var name = $"revision-{++revision}";
                    _names[node.Id] = name;
                    object renamed = node.Level == 1
                        ? new ReplayShapeChildRenamed(node.Parent, node.Id, name)
                        : new ReplayShapeDescendantRenamed(node.Parent, node.Id, name);
                    yield return new(node.Id, renamed);
                }
                if (settings.RunLengthMix is null) remaining[index] -= run;
            }
            firstRound = false;
        }
        foreach (var root in roots.Where(root => _removed.Contains(ChildId(root, settings.Depth))))
        {
            var id = ChildId(root, settings.Depth);
            object removed = settings.Depth == 1
                ? new ReplayShapeChildRemoved(root, id)
                : new ReplayShapeDescendantRemoved(ChildId(root, settings.Depth - 1), id);
            yield return new(id, removed);
        }
    }

    void VerifyChildren(IEnumerable<ReplayShapeChild>? children, string root)
    {
        var items = children?.ToArray() ?? [];
        var id = ChildId(root, 1);
        var expected = !_removed.Contains(id);
        if (items.Length != (expected ? 1 : 0)) throw new InvalidOperationException($"Incorrect child count for {root} at depth 1.");
        if (!expected) return;
        var child = items.Single();
        VerifyNode(child.Id, child.Name, id);
        var leaves = child.Leaves?.ToArray() ?? [];
        var leafId = ChildId(root, 2);
        var expectedLeaf = settings.Depth == 2 && !_removed.Contains(leafId);
        if (leaves.Length != (expectedLeaf ? 1 : 0)) throw new InvalidOperationException($"Incorrect child count for {root} at depth 2.");
        if (expectedLeaf) VerifyNode(leaves.Single().Id, leaves.Single().Name, leafId);
    }

    void VerifyNode(string id, string name, string expectedId)
    {
        if (id != expectedId || name != _names[expectedId]) throw new InvalidOperationException($"Incorrect child identity/name for {expectedId}: got {id}/{name}.");
    }
}
