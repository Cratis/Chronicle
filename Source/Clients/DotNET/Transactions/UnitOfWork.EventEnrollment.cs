// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.Transactions;

public partial class UnitOfWork
{
    /// <inheritdoc/>
    public void AddEventWithOptions(EventSequenceId eventSequenceId, EventSourceId eventSourceId, object @event, Causation causation, EventEnrollmentOptions options)
    {
        ConcurrencyScopeOriginNotValidForEvents.ThrowIfNotValid(options.Origin);
        AddEventWithNamedTagsCore(eventSequenceId, eventSourceId, @event, options.NamedTags ?? [], causation, options.EventStreamType, options.EventStreamId, options.EventSourceType, options.ConcurrencyScope, options.Tags, options.Occurred, options.Subject, options.Origin, options.DeclaredConcurrency);
    }

    void AddEventWithNamedTagsCore(
        EventSequenceId eventSequenceId,
        EventSourceId eventSourceId,
        object @event,
        IEnumerable<NamedTag> namedTags,
        Causation causation,
        EventStreamType? eventStreamType,
        EventStreamId? eventStreamId,
        EventSourceType? eventSourceType,
        ConcurrencyScope? concurrencyScope,
        IEnumerable<string>? tags,
        DateTimeOffset? occurred,
        Subject? subject,
        ConcurrencyScopeOrigin origin,
        ConcurrencyDimensions? declaredConcurrency = null)
    {
        lock (_decisionLock)
        {
            ThrowIfStagingAfterCompletion();
            if (IsEnforcingReadProtection)
            {
                var enforcedEvent = new EventForEventSourceId(eventSourceId, @event, causation)
                {
                    EventStreamType = eventStreamType ?? EventStreamType.All,
                    EventStreamId = eventStreamId ?? EventStreamId.Default,
                    EventSourceType = eventSourceType ?? EventSourceType.Default,
                    Tags = tags ?? [],
                    NamedTags = NamedTagConverters.Merge([], namedTags),
                    Occurred = occurred,
                    Subject = subject,
                    DeclaredConcurrency = declaredConcurrency
                };
                AddEnforcedEvents(eventSequenceId, [enforcedEvent], [new(eventSourceId, concurrencyScope ?? ConcurrencyScope.NotSet)], origin);
                return;
            }
            var scope = concurrencyScope ?? ConcurrencyScope.NotSet;
            var enrollment = new UnitOfWorkEnrollment(eventSequenceId, eventSourceId, scope, Array.AsReadOnly<object>([@event]));
            var resolution = ResolveEnrollmentScopes(eventSequenceId, [enrollment]);
            scope = resolution.Scopes[eventSourceId];
            var stagedEvent = new EventForEventSourceId(eventSourceId, @event, causation)
            {
                EventStreamType = eventStreamType ?? EventStreamType.All,
                EventStreamId = eventStreamId ?? EventStreamId.Default,
                EventSourceType = eventSourceType ?? EventSourceType.Default,
                Tags = tags ?? [],
                NamedTags = NamedTagConverters.Merge([], namedTags),
                Occurred = occurred,
                Subject = subject
            };
            if (_decisionScopes.Count != 0)
            {
                ValidateLegacyEventSequenceIdsForOrderedBatch(eventSequenceId);
                EnsureEventSequenceCanBeUsed(eventSequenceId);
            }
            if (scope != ConcurrencyScope.NotSet && _decisionScopes.ContainsKey(new(eventSourceId.Value)))
            {
                throw new DecisionReadConflictsWithConcurrencyScope(eventSourceId);
            }
            if (_hasOrderedBatch)
            {
                ThrowIfLabelIsNotSpecified(eventSourceId);
                scope = MaterializeConcurrencyScope(scope);
                EnsureEventSequenceCanBeUsed(eventSequenceId);
                ValidateConcurrencyScope(eventSequenceId, eventSourceId, scope, _concurrencyScopes, resolution.Requirements);
                BindToEventSequence(eventSequenceId);
            }
            else
            {
                BindLegacyEventSequence(eventSequenceId);
            }

            if (_currentLegacyEvents is null)
            {
                _currentLegacyEvents = new LegacyStagedEvents();
                _stagedEvents.Add(_currentLegacyEvents);
            }

            _currentLegacyEvents.Add(stagedEvent);
            if (_hasOrderedBatch)
            {
                EnrollStrictConcurrencyScope(eventSourceId, scope);
            }
            else
            {
                SetLegacyConcurrencyScope(eventSourceId, scope);
            }
            CompleteEnrollment(resolution);
        }
    }
}
