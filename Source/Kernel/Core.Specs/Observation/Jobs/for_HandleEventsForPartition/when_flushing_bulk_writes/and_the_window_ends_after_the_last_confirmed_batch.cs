// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_flushing_bulk_writes;

public class and_the_window_ends_after_the_last_confirmed_batch : given.a_buffered_projection_job_step
{
    bool _rangeEnded;
    int _batches;

    void Establish()
    {
        _eventCursor.MoveNext().Returns(_ =>
        {
            _rangeEnded = ++_batches > 1;
            return Task.FromResult(!_rangeEnded);
        });
        _observerServiceClient.FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>())
            .Returns(_ => Task.FromResult(_rangeEnded ? Windows() : Windows(_window)));
    }

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_succeed_with_the_confirmed_progress() => _result.Match(result => !result.TryGetError(out _), _ => false).ShouldBeTrue();
    [Fact] void should_not_flush_again_without_more_progress() => _observerServiceClient.Received(2).FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>());
    [Fact] void should_keep_the_confirmed_checkpoint() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.Value.ShouldEqual(1UL);
}
