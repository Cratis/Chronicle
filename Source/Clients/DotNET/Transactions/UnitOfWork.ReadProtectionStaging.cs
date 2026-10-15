// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions;

public partial class UnitOfWork
{
    static void ValidateEnforcedLabel(EventSourceId label)
    {
        ThrowIfLabelIsNotSpecified(label);
        if (label.Value.StartsWith(ReadProtectionMerge.ReservedLabelPrefix, StringComparison.Ordinal)) throw new ReservedConcurrencyScopeLabel(label);
    }

    void AddEnforcedEvents(EventSequenceId sequence, IEnumerable<EventForEventSourceId> events, IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>> scopes, ConcurrencyScopeOrigin origin)
    {
        var batch = new EventsWithConcurrencyScopes(events, scopes.Select(_ => new KeyValuePair<EventSourceId, ConcurrencyScope>(new(_.Key.Value), _.Value)), true);
        EnsureEventSequenceCanBeUsed(sequence);
        foreach (var label in batch.Events.Select(_ => _.EventSourceId).Union(batch.ConcurrencyScopes.Keys)) ValidateEnforcedLabel(label);
        var targets = batch.Events.GroupBy(_ => new EventSourceId(_.EventSourceId.Value)).ToDictionary(_ => _.Key, _ => Array.AsReadOnly(_.Select(e => e.Event).ToArray()));
        var enrollments = targets.Keys.Union(batch.ConcurrencyScopes.Keys).Select(label => new UnitOfWorkEnrollment(sequence, label, batch.ConcurrencyScopes.GetValueOrDefault(label, ConcurrencyScope.NotSet), targets.GetValueOrDefault(label, Array.AsReadOnly(Array.Empty<object>())))).ToArray();
        var resolution = ResolveEnrollmentScopes(sequence, enrollments);
        var pending = CaptureStagedContributions(sequence, resolution.Scopes, origin, batch.Events.Count == 0);
        var staged = batch.Events.Select(_ => _ with { NamedTags = NamedTagConverters.Merge(_.NamedTags, []) }).ToArray();
        BindToEventSequence(sequence);
        _hasOrderedBatch = true;
        _currentLegacyEvents = null;
        _stagedEvents.Add(new OrderedStagedEvents(staged));
        _readProtectionContributions.AddRange(pending);
        foreach (var (label, scope) in resolution.Scopes) EnrollStrictConcurrencyScope(label, scope);
        CompleteEnrollment(resolution);
    }

    List<ReadProtectionContribution> CaptureStagedContributions(EventSequenceId sequence, IReadOnlyDictionary<EventSourceId, ConcurrencyScope> scopes, ConcurrencyScopeOrigin origin, bool validateOnly)
    {
        var pending = new List<ReadProtectionContribution>();
        foreach (var (label, scope) in scopes.Where(_ => _.Value != ConcurrencyScope.NotSet))
        {
            if (scope.EventSourceId is not null && scope.EventSourceId.Value != label.Value) throw new ConcurrencyScopeEventSourceIdDoesNotMatchLabel(label, scope.EventSourceId);
            var kind = origin == ConcurrencyScopeOrigin.Aggregate ? ReadProtectionContributionKind.Aggregate : ReadProtectionContributionKind.Explicit;
            var intent = validateOnly && kind == ReadProtectionContributionKind.Explicit ? ReadProtectionContributionIntent.ValidateOnly : ReadProtectionContributionIntent.None;
            var contribution = new ReadProtectionContribution(label, kind, scope, new(eventStore.Name, eventStore.Namespace, sequence), $"{origin} scope for '{label}'", intent);
            ValidateReadProtectionTarget(contribution);
            ValidateDistinctReadProtectionContribution(contribution, _readProtectionContributions.Concat(pending));
            pending.Add(contribution);
        }
        return pending;
    }
}
