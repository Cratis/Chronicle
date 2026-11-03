// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpAdmission.when_a_start_is_delayed;

/// <summary>
/// A start granted epoch 1 is held up while the observer reactivates and grants its catch-up claim again, as epoch 2.
/// When the start lands it belongs to nobody, so it must not run.
/// </summary>
public class and_the_observer_reactivates : given.an_admitting_catch_up_job
{
    Result<StartJobError> _result;
    bool _startWaitedForTheRead;

    void Establish() => HoldTheObserverStateRead();

    async Task Because()
    {
        var starting = _job.Start(RequestWithEpoch(1));
        _startWaitedForTheRead = !starting.IsCompleted;
        _observerStateRead!.SetResult(ClaimedByCatchUp(2));
        _result = await starting;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_have_waited_for_the_observer_state() => _startWaitedForTheRead.ShouldBeTrue();
    [Fact] void should_refuse_the_start() => (_result.TryGetError(out var error) && error == StartJobError.NotAdmitted).ShouldBeTrue();
    [Fact] void should_not_persist_the_job() => _stateStorage.State.Request.ShouldBeNull();
    [Fact] void should_not_call_the_observer() => _observer.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_read_the_partitions() => _keyIndexes.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_notify_any_subscriber() => _catchupServiceClient.ReceivedCalls().ShouldBeEmpty();
    [Fact] async Task should_not_register_catching_up_partitions() => await _observer.DidNotReceive().RegisterCatchingUpPartitions(Arg.Any<IEnumerable<Key>>());
}
