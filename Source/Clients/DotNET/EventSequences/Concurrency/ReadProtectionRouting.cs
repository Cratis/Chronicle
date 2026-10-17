// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Resolves routing individually on read labels, retaining today's resolver elsewhere.
/// </summary>
internal static class ReadProtectionRouting
{
    /// <summary>
    /// Resolves the conditions for an internal commit, with transport allowances confined to the default strategy.
    /// </summary>
    /// <param name="commit">The unit's snapshot.</param>
    /// <param name="entries">Resolved entries in batch order.</param>
    /// <param name="scopes">Legacy explicit scopes.</param>
    /// <param name="strategy">The configured strategy.</param>
    /// <returns>The actual append plan.</returns>
    /// <exception cref="ReadProtectionRequiresTheDefaultConcurrencyStrategy">A read label uses a custom strategy.</exception>
    internal static async Task<ReadProtectionPlan> Resolve(
        ReadProtectionCommit commit,
        IReadOnlyList<(EventForEventSourceId Event, ResolvedEventRouting? Routing)> entries,
        IReadOnlyDictionary<EventSourceId, ConcurrencyScope> scopes,
        IConcurrencyScopeStrategy strategy)
    {
        ReadProtectionMerge.ValidateTargetsAndLabels(commit.Target, commit.Contributions);
        var contributions = commit.Contributions.ToList();
        var legacy = scopes.ToDictionary(_ => new EventSourceId(_.Key.Value), _ => _.Value);
        var readLabels = contributions.Where(_ => _.Kind == ReadProtectionContributionKind.ProtectedRead).Select(_ => _.Label).ToHashSet();
        var customLabel = entries.FirstOrDefault(_ => readLabels.Contains(new(_.Event.EventSourceId.Value))).Event?.EventSourceId;
        if (customLabel is not null && strategy.GetType() != typeof(OptimisticConcurrencyStrategy)) throw new ReadProtectionRequiresTheDefaultConcurrencyStrategy(customLabel);
        var cache = new Dictionary<object, ConcurrencyScope>();
        foreach (var group in entries.GroupBy(_ => new EventSourceId(_.Event.EventSourceId.Value)))
        {
            if (!readLabels.Contains(group.Key))
            {
                if (!legacy.TryGetValue(group.Key, out var supplied) || supplied == ConcurrencyScope.NotSet)
                {
                    legacy[group.Key] = await ResolveLegacy(group.Key, group.ToArray(), strategy);
                }
                continue;
            }
            foreach (var (@event, routing) in group)
            {
                var dimensions = @event.DeclaredConcurrency ?? routing?.Dimensions;
                var key = (group.Key, dimensions, @event.EventStreamType, @event.EventStreamId, @event.EventSourceType);
                if (!cache.TryGetValue(key, out var scope))
                {
                    using var allowance = new ReadProtectionReadRegion(ReadAllowance.Contributed, "default commit routing");
                    scope = dimensions is null
                        ? await strategy.GetScope(group.Key, @event.EventStreamType, @event.EventStreamId, @event.EventSourceType)
                        : await strategy.GetScope(dimensions.Value, group.Key, @event.EventStreamType, @event.EventStreamId, @event.EventSourceType);
                    scope = scope with { IssuedByStrategy = true };
                    cache.Add(key, scope);
                }
                var declared = dimensions is not null && dimensions != ConcurrencyDimensions.None;
                contributions.Add(new(group.Key, declared ? ReadProtectionContributionKind.DeclaredRouting : ReadProtectionContributionKind.DefaultRouting, scope, commit.Target, $"routing of {routing?.EventSource.Value ?? @event.Event.GetType().Name}/{@event.EventStreamType}"));
            }
        }
        return ReadProtectionMerge.Apply(commit.Target, contributions, legacy, entries.Count != 0, commit.ValidateReadsWithoutEvents);
    }

    static async Task<ConcurrencyScope> ResolveLegacy(EventSourceId source, IReadOnlyList<(EventForEventSourceId Event, ResolvedEventRouting? Routing)> entries, IConcurrencyScopeStrategy strategy)
    {
        if (strategy.GetType() != typeof(OptimisticConcurrencyStrategy)) return await DefinitionConcurrencyScopeResolver.Resolve(source, entries, strategy, true);
        using var allowance = new ReadProtectionReadRegion(ReadAllowance.Contributed, "default commit routing");
        return await DefinitionConcurrencyScopeResolver.Resolve(source, entries, strategy, true);
    }
}
