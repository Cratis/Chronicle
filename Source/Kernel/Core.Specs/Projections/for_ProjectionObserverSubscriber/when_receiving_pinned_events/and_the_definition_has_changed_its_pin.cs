// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_receiving_pinned_events;

public class and_the_definition_has_changed_its_pin : given.a_pinned_subscriber
{
    ProjectionDefinition _updated;
    ObserverSubscriberResult _result;

    void Establish()
    {
        _projection.EventTypes.Returns([_event.Context.EventType]);
        _projection.Accepts(_event.Context.EventType).Returns(true);
        _updated = _definition with
        {
            GenerationDelivery = EventGenerationDelivery.Pinned,
            From = new Dictionary<EventType, FromDefinition>
            {
                [_pin] = new(new Dictionary<PropertyPath, string> { ["fullName"] = "$eventContent(fullName)" }, "$eventSourceId", string.Empty)
            }
        };
        _factory.Create(_key.EventStore, _key.Namespace, _updated, Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>()).Returns(_ =>
        {
            _projection.EventTypes.Returns([_pin]);
            _projection.Accepts(_event.Context.EventType).Returns(false);
            return _projection;
        });
    }

    async Task Because()
    {
        // SetDefinition notifies this subscriber before the observer's subscription is rebound.
        await _subscriber.OnProjectionDefinitionsChanged(_updated);
        _result = await _subscriber.OnNext("the-partition", [_event], new(null));
    }

    [Fact] void should_apply_the_old_subscription_event_to_the_new_pipeline() => _ = _refreshedPipeline.Received(1).Handle(Arg.Any<AppendedEvent>());
    [Fact] void should_not_apply_it_to_the_stale_pipeline() => _ = _cachedPipeline.DidNotReceive().Handle(Arg.Any<AppendedEvent>());
    [Fact] void should_use_the_current_pin() => _delivered.Context.EventType.ShouldEqual(_pin);
    [Fact] void should_apply_the_migrated_content() => ((IDictionary<string, object?>)_delivered.Content)["fullName"].ShouldEqual("Ada Lovelace");
    [Fact] void should_not_fail_the_partition() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_acknowledge_the_applied_event() => _result.LastSuccessfulObservation.ShouldEqual(_event.Context.SequenceNumber);
}
