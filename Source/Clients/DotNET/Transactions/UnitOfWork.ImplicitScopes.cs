// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.Transactions;

public partial class UnitOfWork
{
    readonly Dictionary<EventSourceId, ScopeOrigins> _scopeOrigins = [];

    /// <summary>
    /// Unlike the original per-label storage proposal, boundaries belong to a sequence as well as a label.
    /// A foreign sequence must neither replace the bound sequence's contribution nor lower its boundary.
    /// </summary>
    readonly Dictionary<(EventSequenceId Sequence, EventSourceId Label), EventSequenceNumber> _implicitScopes = [];

    /// <inheritdoc/>
    public void AddEventWithOptions(EventSequenceId eventSequenceId, EventSourceId eventSourceId, object @event, Causation causation, EventEnrollmentOptions options)
    {
        ConcurrencyScopeOriginNotValidForEvents.ThrowIfNotValid(options.Origin);
        AddEventWithNamedTagsCore(eventSequenceId, eventSourceId, @event, options.NamedTags ?? [], causation, options.EventStreamType, options.EventStreamId, options.EventSourceType, options.ConcurrencyScope, options.Tags, options.Occurred, options.Subject, options.Origin, options.DeclaredConcurrency);
    }

    /// <inheritdoc/>
    /// <exception cref="ConcurrencyScopeLabelMustBeSpecified">The label is unspecified.</exception>
    /// <exception cref="ImplicitScopeBoundaryMustBeConcrete">The boundary is not concrete.</exception>
    /// <exception cref="UnitOfWorkIsCompleted">The strict unit is completing or completed.</exception>
    public void AddImplicitScope(EventSequenceId eventSequenceId, EventSourceId label, EventSequenceNumber boundary)
    {
        lock (_decisionLock)
        {
            ThrowIfMutatedDuringEnrollment();
            ThrowIfEnrollmentNotificationFailed();
            ThrowIfLabelIsNotSpecified(label);
            if (!boundary.IsActualValue && !boundary.IsBeforeFirst)
            {
                throw new ImplicitScopeBoundaryMustBeConcrete(boundary);
            }
            if (_completing || IsCompleted)
            {
                if (lifecyclePolicy == UnitOfWorkLifecyclePolicy.Strict) throw new UnitOfWorkIsCompleted(correlationId);
                if (_logger is not null) _logger.ImplicitScopeAfterCompletion(correlationId);
                else Trace.TraceInformation("keyguard.implicit_after_completion: unit '{0}' ignored an implicit scope.", correlationId);
                return;
            }

            var key = (eventSequenceId, label);
            _implicitScopes[key] = _implicitScopes.TryGetValue(key, out var existing)
                ? ImplicitScopeMerge.Earliest(existing, boundary)
                : boundary;
        }
    }

    static Task<AppendManyResult> AppendWithScopeProvenance(
        IEventSequence sequence,
        EventForEventSourceId[] events,
        Dictionary<EventSourceId, ConcurrencyScope> scopes,
        ScopeProvenance provenance) => sequence is IEventSequenceWithScopeProvenance supported
            ? supported.AppendMany(events, [], scopes, provenance)
            : throw new EventSequenceImplicitScopesNotSupported(sequence.GetType());

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
            if (scope != ConcurrencyScope.NotSet && _decisionScopes.ContainsKey(eventSourceId))
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
            RecordScopeOrigins(resolution, origin);
        }
    }

    void RecordScopeOrigins(EnrollmentResolution resolution, ConcurrencyScopeOrigin origin)
    {
        var flag = origin switch
        {
            ConcurrencyScopeOrigin.Aggregate => ScopeOrigins.Aggregate,
            ConcurrencyScopeOrigin.Routing => ScopeOrigins.Routing,
            _ => ScopeOrigins.Explicit
        };
        foreach (var enrollment in resolution.Enrollments.Where(_ => _.SuppliedScope != ConcurrencyScope.NotSet))
        {
            RecordScopeOrigin(enrollment.EventSourceId, flag);
        }
        foreach (var (label, _) in resolution.Requirements.Where(_ => _.Value != ConcurrencyScope.NotSet))
        {
            RecordScopeOrigin(label, ScopeOrigins.Explicit);
        }
    }

    void RecordScopeOrigin(EventSourceId label, ScopeOrigins origin) =>
        _scopeOrigins[label] = _scopeOrigins.GetValueOrDefault(label) | origin;

    ScopeProvenance CaptureScopeProvenance()
    {
        var foreignCount = _implicitScopes.Count(_ => _.Key.Sequence != _eventSequenceId);
        if (foreignCount != 0)
        {
            ScopeGuardDiagnostics.OtherSequence(foreignCount);
            if (_logger is not null) _logger.ImplicitScopesOnOtherSequence(correlationId, foreignCount);
            else Trace.WriteLine($"keyguard.other_sequence: unit '{correlationId}' ignored {foreignCount} implicit scopes.");
        }
        return new(
            _scopeOrigins.ToDictionary(_ => _.Key, _ => _.Value),
            _decisionScopes.Keys.ToHashSet(),
            _implicitScopes.Where(_ => _.Key.Sequence == _eventSequenceId).ToDictionary(_ => _.Key.Label, _ => _.Value));
    }
}
