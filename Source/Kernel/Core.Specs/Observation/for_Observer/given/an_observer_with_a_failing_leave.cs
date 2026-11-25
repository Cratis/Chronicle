// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_a_failing_leave : an_observer_with_controlled_persistence
{
    protected readonly InvalidOperationException _failure = new("Queue unsubscribe failed");
    protected readonly TaskCompletionSource _leaving = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _leave = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected Exception? _transitionFailure;

    void Establish()
    {
        _jobsManager.ClearReceivedCalls();
        _appendedEventsQueues.Unsubscribe(Arg.Any<AppendedEventsQueueSubscription>()).Returns(_ =>
        {
            _leaving.TrySetResult();
            return _leave.Task;
        });
    }

    protected async Task FailLeave(bool subscribeWhileLeaving)
    {
        var transition = _observer.TransitionTo<Disconnected>();
        try
        {
            await _leaving.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            if (subscribeWhileLeaving) await Subscribe().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
        finally
        {
            _leave.TrySetException(_failure);
        }

        _transitionFailure = await Catch.Exception(() => transition.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));
        _appendedEventsQueues.Unsubscribe(Arg.Any<AppendedEventsQueueSubscription>()).Returns(Task.CompletedTask);
    }
}
