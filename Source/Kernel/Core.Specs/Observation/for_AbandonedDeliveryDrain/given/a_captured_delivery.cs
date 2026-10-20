// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Observation.given;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.for_AbandonedDeliveryDrain.given;

public class a_captured_delivery : Specification
{
    protected readonly DeliveryClock _clock = new();
    protected IGrainFactory _factory;
    protected IReactorObserverSubscriber _subscriber;
    protected ObserverSubscription _subscription;
    protected DeliveryFence _fence;

    void Establish()
    {
        var target = new ObserverSubscriberTarget(SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1), new ConnectedClient { ConnectionId = "A" });
        _subscription = new("observer", new("observer", "store", "namespace", "sequence"), [], typeof(IReactorObserverSubscriber), target.SiloAddress) { Targets = [target] };
        var context = new ObserverSubscriberContext(target.ConnectedClient).WithDeadlines(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(300), _clock);
        var captured = AbandonedDeliveryDrain.Capture(_subscription, "partition", target, context)!;
        _fence = new("partition", captured.ReleaseBy!.Value, captured.Targets!);
        _factory = Substitute.For<IGrainFactory>();
        _subscriber = Substitute.For<IReactorObserverSubscriber>();
        _factory.GetGrain(typeof(IReactorObserverSubscriber), _fence.Targets[0].SubscriberKey).Returns(_subscriber);
    }

    protected Task Drain() => AbandonedDeliveryDrain.Until(_fence, _factory, _clock, NullLogger.Instance, CancellationToken.None);
}
