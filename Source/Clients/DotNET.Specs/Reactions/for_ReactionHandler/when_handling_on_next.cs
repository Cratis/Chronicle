// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.Reactors.for_ReactorHandler;

public class when_handling_on_next : given.a_reactor_handler
{
    EventSequenceNumber event_sequence_number;
    EventMetadata metadata;
    EventContext context;
    IDictionary<string, string> causation_properties;
    SomeEvent event_content;

    void Establish()
    {
        event_sequence_number = 42;

        event_content = new("Forty two");
        metadata = new(0, new(Guid.NewGuid().ToString(), 1));
        context = EventContext.Empty;

        causation_manager
            .Setup(_ => _.Add(ReactorHandler.CausationType, IsAny<IDictionary<string, string>>()))
            .Callback((CausationType _, IDictionary<string, string> properties) => causation_properties = properties);
    }

    async Task Because() => await handler.OnNext(metadata, context, event_content);

    [Fact] void should_add_causation() => causation_manager.Verify(_ => _.Add(ReactorHandler.CausationType, IsAny<IDictionary<string, string>>()), Once);
    [Fact] void should_add_causation_with_observer_id() => causation_properties[ReactorHandler.CausationReactorIdProperty].ShouldEqual(Reactor_id.ToString());
    [Fact] void should_add_causation_with_event_type_id() => causation_properties[ReactorHandler.CausationEventTypeIdProperty].ShouldEqual(metadata.Type.Id.Value);
    [Fact] void should_add_causation_with_event_type_generation() => causation_properties[ReactorHandler.CausationEventTypeGenerationProperty].ShouldEqual(metadata.Type.Generation.ToString());
    [Fact] void should_add_causation_with_event_sequence_id() => causation_properties[ReactorHandler.CausationEventSequenceIdProperty].ShouldEqual(event_sequence_id.ToString());
    [Fact] void should_add_causation_with_event_sequence_number() => causation_properties[ReactorHandler.CausationEventSequenceNumberProperty].ShouldEqual(metadata.SequenceNumber.Value.ToString());
    [Fact] void should_invoke_observer_invoker() => Reactor_invoker.Verify(_ => _.Invoke(event_content, context), Once);
}
