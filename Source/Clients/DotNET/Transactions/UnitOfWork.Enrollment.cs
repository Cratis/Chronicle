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
        first.EventSourceId == second.EventSourceId &&
        Normalize(first.EventStreamType) == Normalize(second.EventStreamType) &&
        Normalize(first.EventStreamId) == Normalize(second.EventStreamId) &&
        Normalize(first.EventSourceType) == Normalize(second.EventSourceType) &&
        (first.EventTypes?.ToHashSet() ?? []).SetEquals(second.EventTypes ?? []);

    static EventStreamType? Normalize(EventStreamType? value) => value == EventStreamType.All ? null : value;
    static EventStreamId? Normalize(EventStreamId? value) => value == EventStreamId.Default ? null : value;
    static EventSourceType? Normalize(EventSourceType? value) => value == EventSourceType.Default || value == EventSourceType.Unspecified ? null : value;

    EnrollmentResolution ResolveEnrollmentScopes(EventSequenceId sequence, IReadOnlyList<UnitOfWorkEnrollment> enrollments)
    {
        var contributors = _enrollmentContributors.ToArray();
        var scopes = new Dictionary<EventSourceId, ConcurrencyScope>();
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
                    if (_concurrencyScopes.TryGetValue(source, out var existing)) MergeRequiredScope(sequence, source, existing, required);
                    requirements[source] = required;
                }
                if (effective != ConcurrencyScope.NotSet && _decisionScopes.ContainsKey(source))
                {
                    throw new DecisionReadConflictsWithConcurrencyScope(source);
                }
                scopes[source] = effective;
            }
            if (requirements.Count != 0 &&
                ((_eventSequenceId is not null && _eventSequenceId != sequence) || _legacyEventSequenceIds.Any(_ => _ != sequence)))
            {
                var (source, required) = requirements.First();
                throw new ConflictingEnrollmentScope(source, scopes.GetValueOrDefault(source, ConcurrencyScope.NotSet), required, sequence);
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
            foreach (var enrollment in resolution.Enrollments.Where(_ => _.Events.Count != 0))
            {
                foreach (var registration in resolution.Contributors)
                {
                    registration.Contributor.Enrolled(enrollment, resolution.Scopes[enrollment.EventSourceId]);
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
        IReadOnlyDictionary<EventSourceId, ConcurrencyScope> Scopes,
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
