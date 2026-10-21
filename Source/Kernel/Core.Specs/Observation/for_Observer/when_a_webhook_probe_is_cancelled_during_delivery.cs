// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_probe_is_cancelled_during_delivery : given.a_webhook_ready_to_probe
{
    TaskCompletionSource _started;
    bool _cancelled;

    void Establish()
    {
        _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _webhook.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            _started.TrySetResult();
            try
            {
                await new TaskCompletionSource().Task.WaitAsync(call.Arg<CancellationToken>());
                return ObserverSubscriberResult.Ok(42UL);
            }
            finally
            {
                _cancelled = true;
            }
        });
    }

    async Task Because()
    {
        var first = Tick();
        await _started.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await Tick();
        await _observer.Unsubscribe();
        await first;
    }

    [Fact] void should_cancel_the_actual_delivery() => _cancelled.ShouldBeTrue();
    [Fact] void should_never_overlap_a_second_delivery() => _webhook.Received(1).OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>());
    [Fact] void should_remove_delivery_authority() => _stateStorage.State.WebhookProbe.ShouldBeNull();
    [Fact] void should_not_advance_the_failed_cursor() => _failedPartitionsState.Partitions.Single().EffectiveRecoveryCursor.Position.ShouldEqual((EventSequenceNumber)42UL);
}
