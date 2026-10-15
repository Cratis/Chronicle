// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Resolves immutable contributions under the Enforce-only n-ary rules.
/// </summary>
internal static class ReadProtectionMerge
{
    /// <summary>
    /// The namespace reserved for additional source-less conditions.
    /// </summary>
    internal const string ReservedLabelPrefix = "$read-protection/";

    /// <summary>
    /// Builds an Enforce commit plan from all acquisitions and resolved routing entries.
    /// </summary>
    /// <param name="target">The unit's full target.</param>
    /// <param name="contributions">The immutable contributions in acquisition and batch order.</param>
    /// <param name="legacyScopes">Today's resolution for labels without protected reads.</param>
    /// <param name="hasEvents">Whether events will be appended.</param>
    /// <returns>The atomic append conditions and their descriptions.</returns>
    internal static ReadProtectionPlan Apply(
        ReadProtectionTarget target,
        IReadOnlyList<ReadProtectionContribution> contributions,
        IReadOnlyDictionary<EventSourceId, ConcurrencyScope> legacyScopes,
        bool hasEvents = true)
    {
        ValidateTargetsAndLabels(target, contributions);
        legacyScopes = legacyScopes.ToDictionary(_ => new EventSourceId(_.Key.Value), _ => _.Value);
        var scopes = new Dictionary<EventSourceId, ConcurrencyScope>();
        var conditions = new List<AppendCondition>();
        foreach (var label in contributions.Select(_ => _.Label).Union(legacyScopes.Keys))
        {
            var items = contributions.Where(_ => _.Label == label).ToArray();
            if (!hasEvents && !items.Any(_ => _.Kind == ReadProtectionContributionKind.Explicit && _.Intent.HasFlag(ReadProtectionContributionIntent.ValidateOnly))) continue;
            ResolveLabel(label, items, legacyScopes, scopes, conditions);
        }
        return new(scopes.ToImmutableDictionary(), conditions.ToImmutableArray());
    }

    /// <summary>
    /// Validates every target before any label is resolved, including labels without reads.
    /// </summary>
    /// <param name="target">The required target.</param>
    /// <param name="contributions">The acquired conditions.</param>
    /// <exception cref="ReadDependencyTargetMismatch">A contribution belongs to another target.</exception>
    /// <exception cref="ReservedConcurrencyScopeLabel">A caller used a reserved label.</exception>
    internal static void ValidateTargetsAndLabels(ReadProtectionTarget target, IEnumerable<ReadProtectionContribution> contributions)
    {
        foreach (var contribution in contributions)
        {
            if (contribution.Target != target) throw new ReadDependencyTargetMismatch(contribution.Description);
            if (contribution.Label.Value.StartsWith(ReservedLabelPrefix, StringComparison.Ordinal)) throw new ReservedConcurrencyScopeLabel(contribution.Label);
        }
    }

    /// <summary>
    /// Deduplicates conditions by meaning and unions validation intent monotonically.
    /// </summary>
    /// <param name="contributions">The conditions in acquisition order.</param>
    /// <returns>The distinct conditions with accumulated intent.</returns>
    internal static IReadOnlyList<ReadProtectionContribution> Distinct(IEnumerable<ReadProtectionContribution> contributions)
    {
        var result = new List<ReadProtectionContribution>();
        foreach (var contribution in contributions)
        {
            var index = result.FindIndex(_ => ReadProtectionPredicate.Same(_.Scope, contribution.Scope));
            if (index < 0) result.Add(contribution);
            else result[index] = result[index] with { Intent = result[index].Intent | contribution.Intent };
        }
        return result.ToImmutableArray();
    }

    static void ResolveLabel(
        EventSourceId label,
        IReadOnlyList<ReadProtectionContribution> items,
        IReadOnlyDictionary<EventSourceId, ConcurrencyScope> legacyScopes,
        IDictionary<EventSourceId, ConcurrencyScope> scopes,
        ICollection<AppendCondition> conditions)
    {
        var reads = items.Where(_ => _.Kind == ReadProtectionContributionKind.ProtectedRead).ToArray();
        if (reads.Length == 0)
        {
            ResolveWithoutReads(label, items, legacyScopes, scopes, conditions);
            return;
        }
        var exact = ValidateReadContributions(label, items, reads);
        var readScope = reads.Select(_ => _.Scope).Aggregate((first, second) => ReadProtectionPredicate.Merge(first, second) with
        {
            SequenceNumber = ReadProtectionPredicate.Earliest(first.SequenceNumber, second.SequenceNumber)
        });
        var condition = ResolveReadCondition(label, reads, exact, readScope);
        var declared = items.Where(_ => _.Kind == ReadProtectionContributionKind.DeclaredRouting).ToArray();
        foreach (var routing in declared.Where(_ => _.Scope.EventSourceId is not null))
        {
            if (exact.Count == 0)
            {
                condition = ReadProtectionPredicate.Merge(condition, routing.Scope);
            }
            else if (!ReadProtectionPredicate.Covers(condition, routing.Scope) || !ReadProtectionPredicate.CoversBoundary(condition, routing.Scope))
            {
                throw new DeclaredRoutingNotCoveredByExplicitScope(label, routing.Description);
            }
        }
        AddCondition(label, condition, items, scopes, conditions);
        KeepSourceLessRouting(label, declared, scopes, conditions);
    }

    static IReadOnlyList<ReadProtectionContribution> ValidateReadContributions(EventSourceId label, IReadOnlyList<ReadProtectionContribution> items, ReadProtectionContribution[] reads)
    {
        var aggregate = items.FirstOrDefault(_ => _.Kind == ReadProtectionContributionKind.Aggregate);
        if (aggregate is not null) throw new ReadAndAggregateOnSameSource(label, reads[0].Description, aggregate.Description);
        if (items.Any(_ => _.Kind == ReadProtectionContributionKind.CustomRouting)) throw new ReadProtectionRequiresTheDefaultConcurrencyStrategy(label);
        var exact = Distinct(items.Where(_ => _.Kind == ReadProtectionContributionKind.Explicit));
        if (exact.Count > 1) throw new ConflictingExplicitScopes(label, exact[0].Description, exact[1].Description);
        return exact;
    }

    static ConcurrencyScope ResolveReadCondition(EventSourceId label, IReadOnlyList<ReadProtectionContribution> reads, IReadOnlyList<ReadProtectionContribution> exact, ConcurrencyScope readScope)
    {
        if (exact.Count == 0) return readScope;
        var scope = exact[0].Scope;
        var coversBoundary = ReadProtectionPredicate.CoversBoundary(scope, readScope);
        if (!ReadProtectionPredicate.Covers(scope, readScope) || !coversBoundary)
        {
            throw new ExplicitScopeDoesNotCoverReads(label, reads.Select(_ => _.Description), !coversBoundary);
        }
        return scope;
    }

    static void ResolveWithoutReads(EventSourceId label, IReadOnlyList<ReadProtectionContribution> items, IReadOnlyDictionary<EventSourceId, ConcurrencyScope> legacyScopes, IDictionary<EventSourceId, ConcurrencyScope> scopes, ICollection<AppendCondition> conditions)
    {
        var exact = Distinct(items.Where(_ => _.Kind is ReadProtectionContributionKind.Explicit or ReadProtectionContributionKind.Aggregate));
        if (exact.Count > 1) throw new ConflictingContributionsOnLabel(label, exact[0].Description, exact[1].Description);
        var scope = exact.Count != 0 ? exact[0].Scope : legacyScopes.GetValueOrDefault(label);
        if (scope is not null) AddCondition(label, ReadProtectionPredicate.Normalize(scope), items, scopes, conditions);
    }

    static void KeepSourceLessRouting(EventSourceId label, IEnumerable<ReadProtectionContribution> declared, IDictionary<EventSourceId, ConcurrencyScope> scopes, ICollection<AppendCondition> conditions)
    {
        var routing = declared.Where(_ => _.Scope.EventSourceId is null).ToArray();
        var seen = new List<ConcurrencyScope>();
        for (var index = 0; index < routing.Length; index++)
        {
            var entry = routing[index];
            if (seen.Exists(_ => ReadProtectionPredicate.Same(_, entry.Scope))) continue;
            seen.Add(entry.Scope);
            var internalLabel = (EventSourceId)$"{ReservedLabelPrefix}routing/{label.Value}/{index}";
            AddCondition(internalLabel, entry.Scope, [entry], scopes, conditions);
        }
    }

    static void AddCondition(EventSourceId label, ConcurrencyScope scope, IEnumerable<ReadProtectionContribution> contributions, IDictionary<EventSourceId, ConcurrencyScope> scopes, ICollection<AppendCondition> conditions)
    {
        var items = contributions.ToArray();
        scopes.Add(label, scope);
        var origins = items.Select(_ => _.Kind switch
        {
            ReadProtectionContributionKind.ProtectedRead => AppendConditionOrigin.ProtectedRead,
            ReadProtectionContributionKind.Explicit => AppendConditionOrigin.Explicit,
            ReadProtectionContributionKind.Aggregate => AppendConditionOrigin.Aggregate,
            ReadProtectionContributionKind.DeclaredRouting => AppendConditionOrigin.DeclaredRouting,
            _ => AppendConditionOrigin.DefaultRouting
        }).Distinct().ToImmutableArray();
        var reads = items.Where(_ => _.Kind == ReadProtectionContributionKind.ProtectedRead).Select(_ => _.Description).Distinct().ToImmutableArray();
        conditions.Add(new(label.Value, scope.EventSourceId, scope.EventTypes!.ToImmutableArray(), scope.EventSourceType, scope.EventStreamType, scope.EventStreamId, scope.SequenceNumber, scope.ExpectsNoMatchingEvent, origins, reads));
    }
}
