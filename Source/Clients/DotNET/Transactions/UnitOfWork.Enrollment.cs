// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions;

public partial class UnitOfWork
{
    readonly List<EnrollmentRegistration> _enrollmentContributors = [];
    readonly Dictionary<EventSourceId, ConcurrencyScope> _requiredEnrollmentScopes = [];
    bool _runningEnrollmentContributors;
    Exception? _enrollmentNotificationFailure;

    /// <inheritdoc/>
    public IDisposable AddEnrollmentContributor(IUnitOfWorkEnrollmentContributor contributor)
    {
        lock (_decisionLock)
        {
            ThrowIfMutatedDuringEnrollment();
            ThrowIfEnrollmentNotificationFailed();
            ThrowIfUnitOfWorkIsCompleted();
            if (_completing) throw new UnitOfWorkIsCompleting(correlationId);
            var registration = new EnrollmentRegistration(this, contributor);
            _enrollmentContributors.Add(registration);
            return registration;
        }
    }

    static ConcurrencyScope MergeRequiredScope(EventSequenceId sequence, EventSourceId source, ConcurrencyScope effective, ConcurrencyScope required)
    {
        if (required == ConcurrencyScope.NotSet) return effective;
        if (effective == ConcurrencyScope.NotSet) return required;
        if (RequiredScopesAreEqual(effective, required)) return effective;
        throw new ConflictingEnrollmentScope(source, effective, required, sequence);
    }

    static bool RequiredScopesAreEqual(ConcurrencyScope first, ConcurrencyScope second) =>
        first.SequenceNumber == second.SequenceNumber &&
        first.EventSourceId?.Value == second.EventSourceId?.Value &&
        Normalize(first.EventStreamType) == Normalize(second.EventStreamType) &&
        Normalize(first.EventStreamId) == Normalize(second.EventStreamId) &&
        Normalize(first.EventSourceType) == Normalize(second.EventSourceType) &&
        (first.EventTypes?.ToHashSet() ?? []).SetEquals(second.EventTypes ?? []);

    static EventStreamType? Normalize(EventStreamType? value) => value == EventStreamType.All ? null : value;
    static EventStreamId? Normalize(EventStreamId? value) => value == EventStreamId.Default ? null : value;
    static EventSourceType? Normalize(EventSourceType? value) => value == EventSourceType.Default || value == EventSourceType.Unspecified ? null : value;

    static UnitOfWorkEnrollment[] CreateEnrollments(EventSequenceId sequence, EventsWithConcurrencyScopes batch, bool useReadProtectionIdentity = false)
    {
        EventSourceId Label(EventSourceId source) => useReadProtectionIdentity ? new(source.Value) : source;
        var targets = batch.Events.GroupBy(_ => Label(_.EventSourceId)).ToDictionary(_ => _.Key, _ => Array.AsReadOnly(_.Select(e => e.Event).ToArray()));
        return targets.Keys.Union(batch.Entries.Select(_ => Label(_.Key))).SelectMany(source =>
        {
            var scopes = batch.Entries.Where(_ => Label(_.Key) == source).Select(_ => _.Value).DefaultIfEmpty(ConcurrencyScope.NotSet);
            return scopes.Select(scope => new UnitOfWorkEnrollment(sequence, source, scope, targets.GetValueOrDefault(source, Array.AsReadOnly(Array.Empty<object>()))));
        }).ToArray();
    }

    EnrollmentResolution ResolveEnrollmentScopes(EventSequenceId sequence, IReadOnlyList<UnitOfWorkEnrollment> enrollments)
    {
        var contributors = _enrollmentContributors.ToArray();
        var scopes = new List<KeyValuePair<EventSourceId, ConcurrencyScope>>();
        var requirements = new Dictionary<EventSourceId, ConcurrencyScope>(_requiredEnrollmentScopes);
        _runningEnrollmentContributors = true;
        try
        {
            foreach (var enrollment in enrollments)
            {
                var source = enrollment.EventSourceId;
                var effective = contributors.Length != 0 || requirements.Count != 0
                    ? MaterializeConcurrencyScope(enrollment.SuppliedScope)
                    : enrollment.SuppliedScope;
                var required = requirements.GetValueOrDefault(source, ConcurrencyScope.NotSet);
                effective = MergeRequiredScope(sequence, source, effective, required);

                // Scope-only entries retain recorded requirements but do not consult contributors.
                if (enrollment.Events.Count != 0)
                {
                    foreach (var registration in contributors)
                    {
                        var contribution = MaterializeConcurrencyScope(registration.Contributor.GetRequiredScope(enrollment));
                        effective = MergeRequiredScope(sequence, source, effective, contribution);
                        required = MergeRequiredScope(sequence, source, required, contribution);
                    }
                }
                if (required != ConcurrencyScope.NotSet)
                {
                    if (_concurrencyScopes.TryGetValue(source, out var existing))
                    {
                        foreach (var scope in existing) MergeRequiredScope(sequence, source, scope, required);
                    }
                    requirements[source] = required;
                }
                if (!IsEnforcingReadProtection && effective != ConcurrencyScope.NotSet && _decisionScopes.ContainsKey(new(source.Value)))
                {
                    throw new DecisionReadConflictsWithConcurrencyScope(source);
                }
                scopes.Add(new(source, effective));
            }
            if (requirements.Count != 0 &&
                ((_eventSequenceId is not null && _eventSequenceId != sequence) || _legacyEventSequenceIds.Any(_ => _ != sequence)))
            {
                var (source, required) = requirements.First();
                throw new ConflictingEnrollmentScope(source, scopes.Find(_ => _.Key == source).Value ?? ConcurrencyScope.NotSet, required, sequence);
            }
            return new(enrollments, contributors, scopes, requirements);
        }
        finally
        {
            _runningEnrollmentContributors = false;
        }
    }

    void CompleteEnrollment(EnrollmentResolution resolution)
    {
        foreach (var (source, scope) in resolution.Requirements) _requiredEnrollmentScopes[source] = scope;
        _runningEnrollmentContributors = true;
        try
        {
            for (var index = 0; index < resolution.Enrollments.Count; index++)
            {
                var enrollment = resolution.Enrollments[index];
                if (enrollment.Events.Count == 0) continue;
                foreach (var registration in resolution.Contributors)
                {
                    registration.Contributor.Enrolled(enrollment, resolution.Scopes[index].Value);
                }
            }
        }
        catch (Exception exception)
        {
            _enrollmentNotificationFailure = exception;
            _appendManyResult = AppendManyResult.Failed(correlationId, [new AppendError(exception.Message)]);
            throw;
        }
        finally
        {
            _runningEnrollmentContributors = false;
        }
    }

    void ThrowIfMutatedDuringEnrollment()
    {
        if (_runningEnrollmentContributors) throw new UnitOfWorkMutatedDuringEnrollment();
    }

    void ThrowIfEnrollmentNotificationFailed()
    {
        if (_enrollmentNotificationFailure is not null) throw new UnitOfWorkEnrollmentNotificationFailed(_enrollmentNotificationFailure);
    }

    sealed record EnrollmentResolution(
        IReadOnlyList<UnitOfWorkEnrollment> Enrollments,
        IReadOnlyList<EnrollmentRegistration> Contributors,
        IReadOnlyList<KeyValuePair<EventSourceId, ConcurrencyScope>> Scopes,
        IReadOnlyDictionary<EventSourceId, ConcurrencyScope> Requirements);

    sealed class EnrollmentRegistration(UnitOfWork unit, IUnitOfWorkEnrollmentContributor contributor) : IDisposable
    {
        public IUnitOfWorkEnrollmentContributor Contributor { get; } = contributor;

        public void Dispose()
        {
            lock (unit._decisionLock)
            {
                unit.ThrowIfMutatedDuringEnrollment();
                unit._enrollmentContributors.Remove(this);
            }
        }
    }
}
