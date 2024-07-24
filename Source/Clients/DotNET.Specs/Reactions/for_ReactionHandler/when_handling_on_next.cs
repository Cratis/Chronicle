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
    EventMetadata metadata;
    EventContext context;
    IDictionary<string, string> causation_properties;
    SomeEvent event_content;

    void Establish()
    {
        event_sequence_number = 42;

        event_content = new("Forty two");
        metadata = new(0, new(Guid.NewGuid().ToString(), 1));
        context = new(Guid.NewGuid(), 0, DateTimeOffset.UtcNow, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Guid.NewGuid().ToString(), [], Identity.NotSet);

        causation_manager
            .Setup(_ => _.Add(ReactionHandler.CausationType, IsAny<IDictionary<string, string>>()))
            .Callback((CausationType _, IDictionary<string, string> properties) => causation_properties = properties);
    }

    async Task Because() => await handler.OnNext(metadata, context, event_content);

    [Fact] void should_add_causation() => causation_manager.Verify(_ => _.Add(ReactionHandler.CausationType, IsAny<IDictionary<string, string>>()), Once);
    [Fact] void should_add_causation_with_observer_id() => causation_properties[ReactionHandler.CausationReactionIdProperty].ShouldEqual(reaction_id.ToString());
    [Fact] void should_add_causation_with_event_type_id() => causation_properties[ReactionHandler.CausationEventTypeIdProperty].ShouldEqual(metadata.Type.Id.Value);
    [Fact] void should_add_causation_with_event_type_generation() => causation_properties[ReactionHandler.CausationEventTypeGenerationProperty].ShouldEqual(metadata.Type.Generation.ToString());
    [Fact] void should_add_causation_with_event_sequence_id() => causation_properties[ReactionHandler.CausationEventSequenceIdProperty].ShouldEqual(event_sequence_id.ToString());
    [Fact] void should_add_causation_with_event_sequence_number() => causation_properties[ReactionHandler.CausationEventSequenceNumberProperty].ShouldEqual(metadata.SequenceNumber.Value.ToString());
    [Fact] void should_invoke_observer_invoker() => reaction_invoker.Verify(_ => _.Invoke(event_content, context), Once);
}
