// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Properties;

using FailedPartition = Cratis.Chronicle.Storage.Sinks.FailedPartition;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_receiving_events;

public class and_a_bulk_flush_fails_a_source_sharing_the_composite_key : given.a_subscriber_receiving_bulk_results
{
    Key _compositeKey;

    void Establish()
    {
        var value = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)value;
        dictionary["Tenant"] = "tenant-1";
        dictionary["Account"] = "account-1";
        _compositeKey = new Key(value, ArrayIndexers.NoIndexers);

        _event = _event with { Context = _event.Context with { EventSourceId = "A" } };
        _cachedPipeline.Handle(Arg.Any<AppendedEvent>()).Returns(call =>
        {
            var context = ContextFor(call.Arg<AppendedEvent>()) with { Key = _compositeKey };
            context.AddFailedPartition(new FailedPartition((Key)"B", 7UL));
            return context;
        });
    }

    async Task Because() => _result = await _subscriber.OnNext((Key)"A", [_event], new(_event.Context.SequenceNumber));

    [Fact] void should_keep_the_delivered_source_successful() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_advance_the_last_handled_event() => _result.LastSuccessfulObservation.ShouldEqual(_event.Context.SequenceNumber);
    [Fact] async Task should_register_the_other_sources_failure() => await _observer.Received(1).PartitionFailed((Key)"B", (EventSequenceNumber)7UL, Arg.Any<IEnumerable<string>>(), string.Empty, FailureKind.Handling);
    [Fact] async Task should_not_register_the_composite_key_as_a_failed_partition() => await _observer.DidNotReceive().PartitionFailed(Arg.Is<Key>(key => key.Value is ExpandoObject), Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<FailureKind>());
}
