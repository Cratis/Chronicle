// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Projections.for_Projections.when_using_registered_source_metadata;

public class and_foreign_store_declarations_match_attributes : an_explicit_store
{
    EventStore _plainStore;
    EventStore _markedStore;
    ProjectionDefinition _plainModel;
    ProjectionDefinition _markedModel;
    ProjectionDefinition _plainFluent;
    ProjectionDefinition _markedFluent;
    (string EventStoreName, IEnumerable<EventTypeId> EventTypeIds) _plainSubscription;
    (string EventStoreName, IEnumerable<EventTypeId> EventTypeIds) _markedSubscription;

    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("foreign-event").ForEventStore("foreign"))
            .RegisterReadModel<PlainModel>().RegisterReadModel<ExplicitPlainModel>()
            .RegisterProjection<FluentModel>(_ => _.From<PlainEvent>(), "inferred-fluent")
            .RegisterProjection<ExplicitFluentModel>(_ => _.FromEventSequence("chosen-sequence").From<PlainEvent>(), "explicit-fluent");
        _plainStore = CreateStore();
        _options = new();
        _options.ExplicitArtifacts.RegisterEventType<MarkedEvent>()
            .RegisterReadModel<MarkedModel>().RegisterReadModel<ExplicitMarkedModel>()
            .RegisterProjection<FluentModel>(_ => _.From<MarkedEvent>(), "inferred-fluent")
            .RegisterProjection<ExplicitFluentModel>(_ => _.FromEventSequence("chosen-sequence").From<MarkedEvent>(), "explicit-fluent");
        _markedStore = CreateStore();
    }

    async Task Because()
    {
        await _plainStore.DiscoverAll();
        await _markedStore.DiscoverAll();
        _plainModel = DefinitionFor(_plainStore, typeof(PlainModel).FullName!);
        _markedModel = DefinitionFor(_markedStore, typeof(MarkedModel).FullName!);
        _plainFluent = DefinitionFor(_plainStore, "inferred-fluent");
        _markedFluent = DefinitionFor(_markedStore, "inferred-fluent");
        _plainSubscription = _plainStore.Projections.GetExternalEventStoreSubscriptions().Single();
        _markedSubscription = _markedStore.Projections.GetExternalEventStoreSubscriptions().Single();
    }

    [Fact] void should_infer_the_foreign_inbox_for_the_plain_model_bound_event() => _plainModel.EventSequenceId.ShouldEqual("inbox-foreign");
    [Fact] void should_match_attributed_model_bound_source_routing() => _plainModel.EventSequenceId.ShouldEqual(_markedModel.EventSequenceId);
    [Fact] void should_infer_the_foreign_inbox_for_the_plain_fluent_event() => _plainFluent.EventSequenceId.ShouldEqual("inbox-foreign");
    [Fact] void should_match_attributed_fluent_source_routing() => _plainFluent.EventSequenceId.ShouldEqual(_markedFluent.EventSequenceId);
    [Fact] void should_keep_model_bound_explicit_sequence_precedence() => DefinitionFor(_plainStore, typeof(ExplicitPlainModel).FullName!).EventSequenceId.ShouldEqual("chosen-sequence");
    [Fact] void should_match_attributed_model_bound_explicit_sequence_precedence() => DefinitionFor(_plainStore, typeof(ExplicitPlainModel).FullName!).EventSequenceId.ShouldEqual(DefinitionFor(_markedStore, typeof(ExplicitMarkedModel).FullName!).EventSequenceId);
    [Fact] void should_keep_fluent_explicit_sequence_precedence() => DefinitionFor(_plainStore, "explicit-fluent").EventSequenceId.ShouldEqual("chosen-sequence");
    [Fact] void should_match_attributed_fluent_explicit_sequence_precedence() => DefinitionFor(_plainStore, "explicit-fluent").EventSequenceId.ShouldEqual(DefinitionFor(_markedStore, "explicit-fluent").EventSequenceId);
    [Fact] void should_include_the_foreign_external_subscription() => _plainSubscription.EventStoreName.ShouldEqual("foreign");
    [Fact] void should_match_the_attributed_external_subscription_store() => _plainSubscription.EventStoreName.ShouldEqual(_markedSubscription.EventStoreName);
    [Fact] void should_subscribe_to_the_registered_event_identity() => _plainSubscription.EventTypeIds.ShouldContainOnly(new EventTypeId("foreign-event"));
    [Fact] void should_match_the_attributed_external_subscription_event_identities() => _plainSubscription.EventTypeIds.ShouldContainOnly(_markedSubscription.EventTypeIds);
    [Fact] void should_keep_public_event_store_helpers_declaration_only() => typeof(PlainEvent).GetEventStoreName().ShouldBeNull();

    static ProjectionDefinition DefinitionFor(IEventStore store, string id) => ((Projections)store.Projections).Definitions.Single(_ => _.Identifier == id);

    [EventType("foreign-event"), EventStore("foreign")]
    public record MarkedEvent(string Name);
    [FromEvent<PlainEvent>]
    public record PlainModel(string Name);
    [FromEvent<MarkedEvent>]
    public record MarkedModel(string Name);
    [FromEvent<PlainEvent>, EventSequence("chosen-sequence")]
    public record ExplicitPlainModel(string Name);
    [FromEvent<MarkedEvent>, EventSequence("chosen-sequence")]
    public record ExplicitMarkedModel(string Name);
    public record FluentModel(string Name);
    public record ExplicitFluentModel(string Name);
}
