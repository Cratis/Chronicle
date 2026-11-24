// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_MultiplexedObservation;

public class when_an_observer_is_slow : given.a_multiplexed_host
{
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    given.OpenStream<global::Cratis.Chronicle.Contracts.Observation.Reactors.ReactorMessage, global::Cratis.Chronicle.Contracts.Observation.EventsToObserve> _slow;
    given.OpenStream<global::Cratis.Chronicle.Contracts.Observation.Reactors.ReactorMessage, global::Cratis.Chronicle.Contracts.Observation.EventsToObserve> _quick;
    int _quickReceivedWhileSlowWasBlocked;
    int _slowIndex;

    async Task Because()
    {
        _slow = new() { OnReceived = _ => _release.Task };
        _slow.Open(_channel.ObserveReactor(_slow.Messages, CancellationToken.None));
        _slow.Messages.OnNext(RegisterReactor("slow"));
        _quick = OpenReactor("quick");
        await WaitForReactors(2);
        _slowIndex = ReactorIdOf(_reactors.Calls[0].Received[0]) == "slow" ? 0 : 1;

        for (var index = 1; index <= 10; index++)
        {
            _reactors.Calls[_slowIndex].Emit(Events($"slow-{index}"));
            _reactors.Calls[1 - _slowIndex].Emit(Events($"quick-{index}"));
        }

        await WaitUntil(() => _quick.Received.Count == 10);
        _quickReceivedWhileSlowWasBlocked = _quick.Received.Count;
        _release.SetResult();
        await WaitUntil(() => _slow.Received.Count == 10);
    }

    void Destroy()
    {
        _slow.Dispose();
        _quick.Dispose();
    }

    [Fact] void should_deliver_to_the_other_observer_while_the_slow_one_holds_a_message() => _quickReceivedWhileSlowWasBlocked.ShouldEqual(10);
    [Fact] void should_deliver_every_message_to_the_slow_observer_in_order_once_it_catches_up() => _slow.Received.Select(_ => _.Partition).ShouldEqual(Enumerable.Range(1, 10).Select(_ => $"slow-{_}"));
}
