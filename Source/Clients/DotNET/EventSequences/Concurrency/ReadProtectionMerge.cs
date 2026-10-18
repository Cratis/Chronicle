// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Resolves immutable contributions into conditions supported by the target kernel.
/// </summary>
internal static class ReadProtectionMerge
{
    /// <summary>
    /// The namespace reserved for internal conditions.
    /// </summary>
    internal const string ReservedLabelPrefix = "$read-protection/";

    /// <summary>
    /// Builds the atomic plan, retaining conservative read guards on kernels without conjunction support.
    /// </summary>
    /// <param name="target">The unit's target.</param>
    /// <param name="contributions">Captured contributions.</param>
    /// <param name="legacyScopes">Scopes for labels without reads.</param>
    /// <param name="hasEvents">Whether events are staged.</param>
    /// <param name="validateReadsWithoutEvents">Whether a receipt requires read validation.</param>
    /// <param name="supportsConjunctions">Whether the kernel accepts several predicates on one label.</param>
    /// <returns>The conditions to send and expose.</returns>
    internal static ReadProtectionPlan Apply(
        ReadProtectionTarget target,
        IReadOnlyList<ReadProtectionContribution> contributions,
        IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>> legacyScopes,
        bool hasEvents = true,
        bool validateReadsWithoutEvents = false,
        bool supportsConjunctions = false)
    {
        ValidateTargetsAndLabels(target, contributions);
        var legacy = legacyScopes.Select(_ => new KeyValuePair<EventSourceId, ConcurrencyScope>(new(_.Key.Value), _.Value)).ToArray();
        var scopes = new List<KeyValuePair<EventSourceId, ConcurrencyScope>>();
        var conditions = new List<AppendCondition>();
        foreach (var label in contributions.Select(_ => _.Label).Union(legacy.Select(_ => _.Key)))
        {
            var items = contributions.Where(_ => _.Label == label).ToArray();
            if (!hasEvents && !validateReadsWithoutEvents && !items.Any(_ => _.Kind == ReadProtectionContributionKind.Explicit && _.Intent.HasFlag(ReadProtectionContributionIntent.ValidateOnly))) continue;
            var resolved = ResolveLabel(label, items, legacy.Where(_ => _.Key == label).Select(_ => _.Value)).ToArray();
            if (supportsConjunctions || !items.Any(_ => _.Kind == ReadProtectionContributionKind.ProtectedRead))
            {
                foreach (var scope in resolved) AddCondition(label, scope, items, scopes, conditions);
                continue;
            }
            foreach (var scope in ResolveLegacyReadScopes(items)) AddCondition(label, scope, items, scopes, conditions);
            AddLegacyRouting(label, items, scopes, conditions);
        }
        return new(scopes.ToImmutableArray(), conditions.ToImmutableArray());
    }

    /// <summary>
    /// Validates target identity and caller labels before resolution.
    /// </summary>
    /// <param name="target">The required target.</param>
    /// <param name="contributions">The captured contributions.</param>
    /// <exception cref="ReadDependencyTargetMismatch">A contribution targets another store or sequence.</exception>
    /// <exception cref="ReservedConcurrencyScopeLabel">A caller label is reserved.</exception>
    internal static void ValidateTargetsAndLabels(ReadProtectionTarget target, IEnumerable<ReadProtectionContribution> contributions)
    {
        foreach (var contribution in contributions)
        {
            if (contribution.Target != target) throw new ReadDependencyTargetMismatch(contribution.Description);
            if (contribution.Label.Value.StartsWith(ReservedLabelPrefix, StringComparison.Ordinal)) throw new ReservedConcurrencyScopeLabel(contribution.Label);
        }
    }

    /// <summary>
    /// Deduplicates identical conditions while retaining validation intent.
    /// </summary>
    /// <param name="contributions">Conditions in acquisition order.</param>
    /// <returns>Distinct immutable contributions.</returns>
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

    /// <summary>
    /// Refuses contradictory exact expectations for the same predicate.
    /// </summary>
    /// <param name="label">The scope label.</param>
    /// <param name="exact">Distinct exact conditions.</param>
    /// <param name="hasReads">Whether protected reads share the label.</param>
    /// <exception cref="ConflictingExplicitScopes">Exact read guards contradict each other.</exception>
    /// <exception cref="ConflictingContributionsOnLabel">Exact guards contradict each other.</exception>
    internal static void ValidateExact(EventSourceId label, IReadOnlyList<ReadProtectionContribution> exact, bool hasReads)
    {
        var none = ReadProtectionPredicate.Normalize(ConcurrencyScope.None);
        for (var index = 0; index < exact.Count; index++)
        {
            var conflicting = exact.Take(index).FirstOrDefault(_ =>
                ReadProtectionPredicate.Same(_.Scope, none) != ReadProtectionPredicate.Same(exact[index].Scope, none) ||
                DefinitionConcurrencyScopeResolver.SamePredicate(_.Scope, exact[index].Scope));
            if (conflicting is null) continue;
            if (hasReads) throw new ConflictingExplicitScopes(label, conflicting.Description, exact[index].Description);
            throw new ConflictingContributionsOnLabel(label, conflicting.Description, exact[index].Description);
        }
    }

    static IEnumerable<ConcurrencyScope> ResolveLabel(EventSourceId label, IReadOnlyList<ReadProtectionContribution> items, IEnumerable<ConcurrencyScope> legacy)
    {
        var reads = items.Where(_ => _.Kind == ReadProtectionContributionKind.ProtectedRead).ToArray();
        var exact = Distinct(items.Where(_ => _.Kind is ReadProtectionContributionKind.Explicit or ReadProtectionContributionKind.Aggregate));
        ValidateExact(label, exact, reads.Length != 0);
        if (reads.Length == 0) return (exact.Count != 0 ? exact.Select(_ => _.Scope) : legacy).Select(ReadProtectionPredicate.Normalize);

        var aggregate = items.FirstOrDefault(_ => _.Kind == ReadProtectionContributionKind.Aggregate);
        if (aggregate is not null) throw new ReadAndAggregateOnSameSource(label, reads[0].Description, aggregate.Description);
        if (items.Any(_ => _.Kind == ReadProtectionContributionKind.CustomRouting)) throw new ReadProtectionRequiresTheDefaultConcurrencyStrategy(label);
        foreach (var read in reads)
        {
            if (exact.Count != 0 && !exact.Any(_ => ReadProtectionPredicate.Covers(_.Scope, read.Scope) && ReadProtectionPredicate.CoversBoundary(_.Scope, read.Scope)))
            {
                throw new ExplicitScopeDoesNotCoverReads(label, reads.Select(_ => _.Description), !exact.Any(_ => ReadProtectionPredicate.CoversBoundary(_.Scope, read.Scope)));
            }
        }
        var declared = items.Where(_ => _.Kind == ReadProtectionContributionKind.DeclaredRouting).ToArray();
        foreach (var routing in declared.Where(_ => _.Scope.EventSourceId is not null))
        {
            if (exact.Count != 0 && !exact.Any(_ => ReadProtectionPredicate.Covers(_.Scope, routing.Scope) && ReadProtectionPredicate.CoversBoundary(_.Scope, routing.Scope)))
            {
                throw new DeclaredRoutingNotCoveredByExplicitScope(label, routing.Description);
            }
        }
        return Coalesce(exact.Count != 0
            ? exact.Select(_ => _.Scope).Concat(declared.Where(_ => _.Scope.EventSourceId is null).Select(_ => _.Scope))
            : reads.Concat(declared).Select(_ => _.Scope));
    }

    static ConcurrencyScope[] ResolveLegacyReadScopes(IReadOnlyList<ReadProtectionContribution> items)
    {
        // Without kernel conjunction support the scopes are chosen by contribution kind, never by whether a scope
        // names an event source: every exact guard is kept as given (a source-less one is a valid global guard),
        // and only source-less declared routing is left to its reserved labels.
        var exact = Distinct(items.Where(_ => _.Kind is ReadProtectionContributionKind.Explicit or ReadProtectionContributionKind.Aggregate));
        if (exact.Count != 0) return [.. exact.Select(_ => _.Scope)];
        var sourceScopes = Coalesce(items
            .Where(_ => _.Kind == ReadProtectionContributionKind.ProtectedRead ||
                        (_.Kind == ReadProtectionContributionKind.DeclaredRouting && _.Scope.EventSourceId is not null))
            .Select(_ => _.Scope));
        return [sourceScopes.Aggregate((first, second) => ReadProtectionPredicate.Merge(first, second) with
        {
            SequenceNumber = ReadProtectionPredicate.Earliest(first.SequenceNumber, second.SequenceNumber)
        })];
    }

    static void AddLegacyRouting(EventSourceId label, IReadOnlyList<ReadProtectionContribution> items, List<KeyValuePair<EventSourceId, ConcurrencyScope>> scopes, List<AppendCondition> conditions)
    {
        var routing = items.Where(_ => _.Kind == ReadProtectionContributionKind.DeclaredRouting && _.Scope.EventSourceId is null).ToArray();
        for (var index = 0; index < routing.Length; index++)
        {
            var entry = routing[index];
            if (routing.Take(index).Any(_ => ReadProtectionPredicate.Same(_.Scope, entry.Scope))) continue;
            AddCondition(new($"{ReservedLabelPrefix}routing/{label.Value}/{index}"), entry.Scope, [entry], scopes, conditions);
        }
    }

    static List<ConcurrencyScope> Coalesce(IEnumerable<ConcurrencyScope> scopes)
    {
        var result = new List<ConcurrencyScope>();
        foreach (var scope in scopes)
        {
            var index = result.FindIndex(_ => DefinitionConcurrencyScopeResolver.SamePredicate(_, scope));
            if (index < 0) result.Add(scope);
            else result[index] = result[index] with { SequenceNumber = ReadProtectionPredicate.Earliest(result[index].SequenceNumber, scope.SequenceNumber) };
        }
        return result;
    }

    static void AddCondition(EventSourceId label, ConcurrencyScope scope, IEnumerable<ReadProtectionContribution> contributions, List<KeyValuePair<EventSourceId, ConcurrencyScope>> scopes, List<AppendCondition> conditions)
    {
        var items = contributions.Where(_ => ReadProtectionPredicate.Same(scope, _.Scope) || (ReadProtectionPredicate.Covers(scope, _.Scope) && ReadProtectionPredicate.CoversBoundary(scope, _.Scope))).ToArray();
        scopes.Add(new(label, scope));
        var origins = items.Select(_ => _.Kind switch
        {
            ReadProtectionContributionKind.ProtectedRead => AppendConditionOrigin.ProtectedRead,
            ReadProtectionContributionKind.Explicit => AppendConditionOrigin.Explicit,
            ReadProtectionContributionKind.Aggregate => AppendConditionOrigin.Aggregate,
            ReadProtectionContributionKind.DeclaredRouting => AppendConditionOrigin.DeclaredRouting,
            _ => AppendConditionOrigin.DefaultRouting
        }).Distinct().ToImmutableArray();
        var reads = items.Where(_ => _.Kind == ReadProtectionContributionKind.ProtectedRead).Select(_ => _.Description).Distinct().ToImmutableArray();
        conditions.Add(new(label.Value, scope.EventSourceId, (scope.EventTypes ?? []).ToImmutableArray(), scope.EventSourceType, scope.EventStreamType, scope.EventStreamId, scope.SequenceNumber, scope.ExpectsNoMatchingEvent, origins, reads));
    }
}
