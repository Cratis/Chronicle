// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Client;
using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Aggregates.for_AggregateRootStateFactory.given;

public class an_aggregate_root_factory : Specification
{
    protected Mock<IAggregateRootEventHandlersFactory> event_handlers_factory;
    protected Mock<IAggregateRootEventHandlers> event_handlers;
    protected Mock<IAggregateRootStateProviders> state_providers;
    protected Mock<IAggregateRootStateProvider> state_provider;
    protected Mock<ICausationManager> causation_manager;
    protected Mock<IEventSequence> event_sequence;
    protected Mock<IEventSequences> event_sequences;
    protected Mock<IEventSerializer> event_serializer;
    protected Mock<IServiceProvider> service_provider;
    protected AggregateRootFactory factory;

    protected IEnumerable<EventType> event_types;
    protected FirstEventType first_event;
    protected SecondEventType second_event;
    protected IEnumerable<AppendedEvent> appended_events;

    void Establish()
    {
        event_types = new EventType[]
        {
            FirstEventType.EventTypeId,
            SecondEventType.EventTypeId
        };
        first_event = new("First");
        second_event = new("Second");

        event_handlers_factory = new();
        event_handlers = new();
        event_handlers.Setup(_ => _.EventTypes).Returns(event_types.ToImmutableList());
        event_handlers_factory.Setup(_ => _.CreateFor(IsAny<IAggregateRoot>())).Returns(event_handlers.Object);

        state_providers = new();
        state_providers.Setup(_ => _.CreateFor(IsAny<AggregateRoot>())).ReturnsAsync(NullAggregateRootStateProvider.Instance);
        state_provider = new();
        causation_manager = new();
        event_sequence = new();

        appended_events = new AppendedEvent[]
        {
            AppendedEvent.EmptyWithEventType(FirstEventType.EventTypeId),
            AppendedEvent.EmptyWithEventType(SecondEventType.EventTypeId)
        };

        event_sequence
            .Setup(_ => _.GetForEventSourceIdAndEventTypes(IsAny<EventSourceId>(), IsAny<IEnumerable<EventType>>()))
            .ReturnsAsync(appended_events.ToImmutableList());

        event_sequences = new();
        event_sequences.Setup(_ => _.GetEventSequence(IsAny<EventSequenceId>())).Returns(event_sequence.Object);

        event_serializer = new();
        event_serializer.SetupSequence(_ => _.Deserialize(IsAny<AppendedEvent>()))
                        .ReturnsAsync(first_event)
                        .ReturnsAsync(second_event);

        service_provider = new();

        factory = new(
            state_providers.Object,
            event_handlers_factory.Object,
            causation_manager.Object,
            event_sequences.Object,
            event_serializer.Object,
            service_provider.Object);
    }
}
