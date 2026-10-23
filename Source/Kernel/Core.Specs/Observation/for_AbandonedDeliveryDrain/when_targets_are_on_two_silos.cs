// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Reactors.Clients;

namespace Cratis.Chronicle.Observation.for_AbandonedDeliveryDrain;

public class when_targets_are_on_two_silos : given.a_captured_delivery
{
    readonly TaskCompletionSource _secondCompletion = new();
    bool _held;

    void Establish()
    {
        var silo = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 22222), 2);
        var key = _subscription.GetSubscriberKeyFor("partition", silo);
        var second = new DeliveryFenceTarget(typeof(IReactorObserverSubscriber).AssemblyQualifiedName!, key.ToString(), "B", silo.ToParsableString());
        _fence = _fence with { Targets = [_fence.Targets[0], second] };
        var subscriber = Substitute.For<IReactorObserverSubscriber>();
        subscriber.Drain().Returns(_secondCompletion.Task);
        _factory.GetGrain(typeof(IReactorObserverSubscriber), key.ToString()).Returns(subscriber);
    }

    async Task Because()
    {
        var drain = Drain();
        _held = !drain.IsCompleted;
        _secondCompletion.SetResult();
        await drain;
    }

    [Fact] void should_wait_for_the_second_original_silo() => _held.ShouldBeTrue();
}
