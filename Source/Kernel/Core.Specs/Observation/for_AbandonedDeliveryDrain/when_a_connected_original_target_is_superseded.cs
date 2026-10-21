// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Observation.Reactors.Clients;

namespace Cratis.Chronicle.Observation.for_AbandonedDeliveryDrain;

public class when_a_connected_original_target_is_superseded : given.a_captured_delivery
{
    readonly TaskCompletionSource _originalCompletion = new();
    IReactorObserverSubscriber _replacement;
    bool _held;

    void Establish()
    {
        _subscriber.Drain().Returns(_originalCompletion.Task);
        var replacementTarget = new ObserverSubscriberTarget(SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 22222), 2), new ConnectedClient { ConnectionId = "B" });
        _subscription = _subscription with { Targets = [replacementTarget], SiloAddress = replacementTarget.SiloAddress };
        _replacement = Substitute.For<IReactorObserverSubscriber>();
        _factory.GetGrain(typeof(IReactorObserverSubscriber), _subscription.GetSubscriberKeyFor("partition", replacementTarget.SiloAddress).ToString()).Returns(_replacement);
    }

    async Task Because()
    {
        var drain = Drain();
        _held = !drain.IsCompleted;
        _originalCompletion.SetResult();
        await drain;
    }

    [Fact] void should_hold_while_A_is_pending_even_though_B_is_idle() => _held.ShouldBeTrue();
    [Fact] void should_drain_A() => _subscriber.Received(1).Drain();
    [Fact] void should_never_substitute_B_for_A() => _replacement.DidNotReceive().Drain();
}
