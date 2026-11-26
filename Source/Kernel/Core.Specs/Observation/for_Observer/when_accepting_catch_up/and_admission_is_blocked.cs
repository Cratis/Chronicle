// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.when_accepting_catch_up;

public class and_admission_is_blocked : given.an_observer_accepting_a_catch_up_handover
{
    readonly given.held_storage_operation<IDisposable?> _admit = new();
    ICatchUpAdmission _admission;
    ObserverState _accepted;
    bool _admittedInline;

    protected override ICatchUpAdmission CreateCatchUpAdmission() => _admission = base.CreateCatchUpAdmission();

    void Establish()
    {
        _admission.TryAdmit(Arg.Any<ObserverKey>(), Arg.Any<int>(), Arg.Any<ulong>()).Returns(_ => _admit.Invoke());
        _admission.ClearReceivedCalls();
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)13UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns((EventSequenceNumber)13UL);
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
    }

    async Task Because()
    {
        Task? continuation = null;
        try
        {
            await _observer.CaughtUp(JobId.New(), 12UL, 12UL).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _accepted = _writes.PersistedState;
            _admittedInline = _admission.ReceivedCalls().Any();
            continuation = _silo.RunCatchUpContinuation();
            await _admit.WaitForStart();
        }
        finally
        {
            _admit.Complete(Substitute.For<IDisposable>());
            if (continuation is not null) await continuation.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
    }

    [Fact] void should_acknowledge_before_admission() => _admittedInline.ShouldBeFalse();
    [Fact] void should_accept_the_cursor_while_admission_is_unavailable() => _accepted.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)13UL);
    [Fact] void should_accept_the_repair_while_admission_is_unavailable() => _accepted.PendingCatchUpRepairs!.ShouldContainOnly(new PendingCatchUpRepair("B", 10UL, 13UL));
}
