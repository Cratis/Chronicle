// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing_a_bucket;

public class and_the_hash_version_is_unknown : given.a_performing_job_step
{
    Catch<JobStepResult> _result;

    void Establish() => _performState.Bucket = new(0, 8, 2);

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_reject_the_unknown_version()
    {
        _result.TryGetException(out var exception).ShouldBeTrue();
        exception.ShouldBeOfExactType<NotSupportedException>();
        exception.Message.ShouldEqual("Replay bucket hash version 2 is not supported.");
    }

    [Fact] void should_not_open_a_cursor() => _eventSequenceStorage.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(_eventSequenceStorage.GetRange)).ShouldEqual(0);
    [Fact] void should_not_dispatch_events() => _handledBatches.ShouldBeEmpty();
    [Fact] void should_not_advance_the_checkpoint() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
