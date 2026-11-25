// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions;

public partial class UnitOfWork
{
    EventsWithConcurrencyScopes CreateRequiredScopeBatch(
        EventSequenceId eventSequenceId,
        IEnumerable<EventForEventSourceId> events,
        IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>> concurrencyScopes)
    {
        var suppliedScopes = concurrencyScopes.Select(pair => new KeyValuePair<EventSourceId, ConcurrencyScope>(
            pair.Key, MaterializeConcurrencyScope(pair.Value))).ToArray();
        return new EventsWithConcurrencyScopes(events, suppliedScopes.Select(pair =>
            _requiredEnrollmentScopes.TryGetValue(pair.Key, out var required)
                ? new KeyValuePair<EventSourceId, ConcurrencyScope>(pair.Key, MergeRequiredScope(eventSequenceId, pair.Key, pair.Value, required))
                : pair));
    }

    void ThrowIfResolvedScopesConflictWithDecisionReads(EnrollmentResolution resolution)
    {
        foreach (var (label, scope) in resolution.Scopes)
        {
            if (scope != ConcurrencyScope.NotSet && _decisionScopes.ContainsKey(new(label.Value)))
            {
                throw new DecisionReadConflictsWithConcurrencyScope(label);
            }
        }
    }

    Dictionary<EventSourceId, List<ConcurrencyScope>> ValidateOrderedBatch(EventSequenceId eventSequenceId, EnrollmentResolution resolution)
    {
        ValidateLegacyEventSequenceIdsForOrderedBatch(eventSequenceId);
        EnsureEventSequenceCanBeUsed(eventSequenceId);
        ValidateExistingEventTargetsForOrderedBatch();
        var materializedConcurrencyScopes = MaterializeConcurrencyScopes(_concurrencyScopes);
        ValidateExistingConcurrencyScopesForOrderedBatch(materializedConcurrencyScopes);
        foreach (var (label, scope) in resolution.Scopes)
        {
            ValidateConcurrencyScope(eventSequenceId, label, scope, materializedConcurrencyScopes, resolution.Requirements);
        }

        return materializedConcurrencyScopes;
    }

    EventForEventSourceId[] ResolveBatchTags(EventSequenceId eventSequenceId, EventsWithConcurrencyScopes batch) =>
        batch.Events.Select(_ => _ with
        {
            NamedTags = ResolveTags(eventSequenceId, _.EventSourceId, _.Event, _.NamedTags)
        }).ToArray();
}
