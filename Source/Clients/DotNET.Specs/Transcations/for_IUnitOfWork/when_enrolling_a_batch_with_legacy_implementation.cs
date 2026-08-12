// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_IUnitOfWork;

public class when_enrolling_a_batch_with_legacy_implementation : Specification
{
    IUnitOfWork _unitOfWork;
    Exception _error;

    void Establish() => _unitOfWork = new LegacyUnitOfWork();

    void Because() => _error = Catch.Exception(() => _unitOfWork.AddEvents(EventSequenceId.Log, [], []));

    [Fact] void should_fail_loudly_instead_of_losing_the_batch() => _error.ShouldBeOfExactType<UnitOfWorkBatchEnrollmentNotSupported>();

    sealed class LegacyUnitOfWork : IUnitOfWork
    {
        public bool IsCompleted => false;
        public CorrelationId CorrelationId => CorrelationId.NotSet;
        public bool IsSuccess => true;

        public void AddEvent(
            EventSequenceId eventSequenceId,
            EventSourceId eventSourceId,
            object @event,
            Causation causation,
            EventStreamType? eventStreamType = default,
            EventStreamId? eventStreamId = default,
            EventSourceType? eventSourceType = default,
            ConcurrencyScope? concurrencyScope = default,
            IEnumerable<string>? tags = default,
            DateTimeOffset? occurred = default,
            Subject? subject = default)
        {
        }

        public IEnumerable<object> GetEvents() => [];
        public IEnumerable<ConstraintViolation> GetConstraintViolations() => [];
        public IEnumerable<ConcurrencyViolation> GetConcurrencyViolations() => [];
        public IEnumerable<AppendError> GetAppendErrors() => [];
        public Task Commit() => Task.CompletedTask;
        public Task Rollback() => Task.CompletedTask;
        public void OnCompleted(Action<IUnitOfWork> callback)
        {
        }

        public bool TryGetLastCommittedEventSequenceNumber([NotNullWhen(true)] out EventSequenceNumber? eventSequenceNumber)
        {
            eventSequenceNumber = null;
            return false;
        }

        public void Dispose()
        {
        }
    }
}
