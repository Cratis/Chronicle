// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class with_signing_after_generation_release : given.a_subscriber_with_signing
{
    AppendedEvent _released;

    void Establish()
    {
        var incoming = AppendedEvent.EmptyWithEventSequenceNumber(42);
        _events = [incoming with { Context = incoming.Context with { EventType = new("customer", 2) } }];
        _released = incoming with { Context = incoming.Context with { EventType = new("customer", 1) } };
        _definitions.Get("webhook").Returns(_definition with { GenerationDelivery = EventGenerationDelivery.Pinned, EventTypes = [new("customer", 1)] });
        _release.Release(Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>(), Arg.Any<IEnumerable<AppendedEvent>>()).Returns([_released]);
    }

    Task Because() => _subscriber.OnNext("partition", _events, new(null));

    [Fact] void should_identify_the_released_batch() => _identities.Single()!.Batches.Single().First.ShouldEqual(_released.Context.SequenceNumber);
    [Fact] void should_deliver_the_released_batch() => _batches.Single().Single().ShouldEqual(_released);
    [Fact] void should_deliver_a_materialized_array() => _batches.Single().ShouldBeOfExactType<AppendedEvent[]>();
}
