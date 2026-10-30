// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_flushing_reducer_writes;

public class and_the_window_is_confirmed : given.a_buffered_reducer_job_step
{
    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_confirm_reducer_replay_writes() => (_observerServiceClient.ReceivedCalls().Count(call =>
        call.GetMethodInfo().Name == nameof(IObserverServiceClient.FlushAllFor) &&
        call.GetArguments()[0] is ObserverDetails { Type: ObserverType.Reducer } && Equals(call.GetArguments()[1], true)) >= 2).ShouldBeTrue();
    [Fact] void should_checkpoint_the_handled_batch() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.Value.ShouldEqual(1UL);
    [Fact] void should_succeed() => _result.Match(result => !result.TryGetError(out _), _ => false).ShouldBeTrue();
}
