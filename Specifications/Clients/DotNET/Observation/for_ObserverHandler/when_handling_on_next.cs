// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Identities;

namespace Aksio.Cratis.Observation.for_ObserverHandler;

public record SomeEvent(string Property);

public class when_handling_on_next : given.an_observer_handler
{
    EventSequenceNumber event_sequence_number;
    AppendedEvent appended_event;
    IDictionary<string, string> causation_properties;
    SomeEvent event_content;

    void Establish()
    {
        event_sequence_number = 42;

        event_content = new("Forty two");

        appended_event = new(
            new(0,
            new(Guid.NewGuid(), 1)),
            new(Guid.NewGuid(), 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid().ToString(), Enumerable.Empty<Causation>(), Identity.NotSet),
            new ExpandoObject());

        event_serializer.Setup(_ => _.Deserialize(appended_event)).Returns(Task.FromResult<object>(event_content));
        causation_manager
            .Setup(_ => _.Add(ObserverHandler.CausationType, IsAny<IDictionary<string, string>>()))
            .Callback((CausationType _, IDictionary<string, string> properties) => causation_properties = properties);
    }

    async Task Because() => await handler.OnNext(appended_event);

    [Fact] void should_add_causation() => causation_manager.Verify(_ => _.Add(ObserverHandler.CausationType, IsAny<IDictionary<string, string>>()), Once);
    [Fact] void should_add_causation_with_observer_id() => causation_properties[ObserverHandler.CausationObserverIdProperty].ShouldEqual(observer_id.ToString());
    [Fact] void should_add_causation_with_event_type_id() => causation_properties[ObserverHandler.CausationEventTypeIdProperty].ShouldEqual(appended_event.Metadata.Type.Id.Value.ToString());
    [Fact] void should_add_causation_with_event_type_generation() => causation_properties[ObserverHandler.CausationEventTypeGenerationProperty].ShouldEqual(appended_event.Metadata.Type.Generation.ToString());
    [Fact] void should_add_causation_with_event_sequence_id() => causation_properties[ObserverHandler.CausationEventSequenceIdProperty].ShouldEqual(event_sequence_id.ToString());
    [Fact] void should_add_causation_with_event_sequence_number() => causation_properties[ObserverHandler.CausationEventSequenceNumberProperty].ShouldEqual(appended_event.Metadata.SequenceNumber.Value.ToString());
    [Fact] void should_invoke_observer_invoker() => observer_invoker.Verify(_ => _.Invoke(event_content, appended_event.Context), Once);
}
