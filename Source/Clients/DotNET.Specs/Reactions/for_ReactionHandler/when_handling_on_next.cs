// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

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

        _causationManager
            .When(_ => _.Add(ReactorHandler.CausationType, Arg.Any<IDictionary<string, string>>()))
            .Do(callInfo => causation_properties = callInfo.Arg<IDictionary<string, string>>());
    }

    async Task Because() => await handler.OnNext(metadata, context, event_content, _serviceProvider);

    [Fact] void should_add_causation() => _causationManager.Received(1).Add(ReactorHandler.CausationType, Arg.Any<IDictionary<string, string>>());
    [Fact] void should_add_causation_with_observer_id() => causation_properties[ReactorHandler.CausationReactorIdProperty].ShouldEqual(_reactorId.ToString());
    [Fact] void should_add_causation_with_event_type_id() => causation_properties[ReactorHandler.CausationEventTypeIdProperty].ShouldEqual(metadata.Type.Id.Value);
    [Fact] void should_add_causation_with_event_type_generation() => causation_properties[ReactorHandler.CausationEventTypeGenerationProperty].ShouldEqual(metadata.Type.Generation.ToString());
    [Fact] void should_add_causation_with_event_sequence_id() => causation_properties[ReactorHandler.CausationEventSequenceIdProperty].ShouldEqual(_eventSequenceId.ToString());
    [Fact] void should_add_causation_with_event_sequence_number() => causation_properties[ReactorHandler.CausationEventSequenceNumberProperty].ShouldEqual(metadata.SequenceNumber.Value.ToString());
    [Fact] void should_invoke_observer_invoker() => _reactorInvoker.Received(1).Invoke(_serviceProvider, event_content, context);
}
