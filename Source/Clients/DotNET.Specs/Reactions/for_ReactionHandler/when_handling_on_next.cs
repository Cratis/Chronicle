// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.Reactions.for_ReactionHandler;

public class when_handling_on_next : given.an_reaction_handler
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
            new(Guid.NewGuid().ToString(), 1)),
            new(Guid.NewGuid(), 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Guid.NewGuid().ToString(), [], Identity.NotSet),
            new ExpandoObject());

        event_serializer.Setup(_ => _.Deserialize(appended_event)).Returns(Task.FromResult<object>(event_content));
        causation_manager
            .Setup(_ => _.Add(ReactionHandler.CausationType, IsAny<IDictionary<string, string>>()))
            .Callback((CausationType _, IDictionary<string, string> properties) => causation_properties = properties);
    }

    async Task Because() => await handler.OnNext(appended_event.Metadata, appended_event.Context, appended_event.Content);

    [Fact] void should_add_causation() => causation_manager.Verify(_ => _.Add(ReactionHandler.CausationType, IsAny<IDictionary<string, string>>()), Once);
    [Fact] void should_add_causation_with_observer_id() => causation_properties[ReactionHandler.CausationReactionIdProperty].ShouldEqual(reaction_id.ToString());
    [Fact] void should_add_causation_with_event_type_id() => causation_properties[ReactionHandler.CausationEventTypeIdProperty].ShouldEqual(appended_event.Metadata.Type.Id.Value);
    [Fact] void should_add_causation_with_event_type_generation() => causation_properties[ReactionHandler.CausationEventTypeGenerationProperty].ShouldEqual(appended_event.Metadata.Type.Generation.ToString());
    [Fact] void should_add_causation_with_event_sequence_id() => causation_properties[ReactionHandler.CausationEventSequenceIdProperty].ShouldEqual(event_sequence_id.ToString());
    [Fact] void should_add_causation_with_event_sequence_number() => causation_properties[ReactionHandler.CausationEventSequenceNumberProperty].ShouldEqual(appended_event.Metadata.SequenceNumber.Value.ToString());
    [Fact] void should_invoke_observer_invoker() => reaction_invoker.Verify(_ => _.Invoke(event_content, appended_event.Context), Once);
}
