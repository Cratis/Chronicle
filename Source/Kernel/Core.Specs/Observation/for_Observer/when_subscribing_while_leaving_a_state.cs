// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_subscribing_while_leaving_a_state : given.an_observer_with_controlled_persistence
{
    readonly TaskCompletionSource _leaving = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _leave = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish()
    {
        _appendedEventsQueues.Unsubscribe(Arg.Any<AppendedEventsQueueSubscription>()).Returns(_ =>
        {
            _leaving.TrySetResult();
            return _leave.Task;
        });
    }

    async Task Because()
    {
        var transition = _observer.TransitionTo<Disconnected>();
        try
        {
            await _leaving.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            await Subscribe().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
        finally
        {
            _leave.TrySetResult();
        }
        await transition.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] async Task should_recover_the_subscription_after_leaving() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
}
