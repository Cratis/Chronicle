// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;
using ConceptsEventStoreName = Cratis.Chronicle.Concepts.EventStoreName;
using context = Cratis.Chronicle.Kernel.Integration.EventTypeResolution.for_EventTypeResolution.when_resolving_registered_event_types.context;

namespace Cratis.Chronicle.Kernel.Integration.EventTypeResolution.for_EventTypeResolution;

/// <summary>
/// Pins how event types resolve against the real storage: registration, generations and migrations, schema lookup, the
/// unknown type, re-registration with and without changes and the separation between event stores. Looking a type up
/// without a generation resolves its first generation, and a change to a description alone is not a schema change.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(ChronicleCollection.Name)]
public class when_resolving_registered_event_types(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public EventTypeSchema Latest = default!;
        public EventTypeSchema FirstGeneration = default!;
        public EventTypeSchema SecondGeneration = default!;
        public EventTypeDefinition Definition = default!;
        public bool HasFirst;
        public bool HasSecond;
        public bool HasUnknownGeneration;
        public bool HasUnknownType;
        public bool UnchangedRegistrationMutated;
        public bool ChangedRegistrationMutated;
        public EventTypeSchema AfterChange = default!;
        public EventTypeSchema AfterTombstone = default!;
        public Exception? UnknownTypeFailure;
        public bool OtherStoreHasIt;
        public int MigrationCount;
        public IEnumerable<EventTypeSchema> LatestForAll = default!;
        public IEnumerable<EventTypeSchema> ByIds = default!;

        public EventTypeId EventTypeId = default!;

        public override bool AutoDiscoverArtifacts => false;

        IEventTypesStorage _eventTypes = default!;
        IEventTypesStorage _otherStore = default!;

        async Task Establish()
        {
            var storage = Services.GetRequiredService<IStorage>();
            _eventTypes = storage.GetEventStore((ConceptsEventStoreName)Constants.EventStore).EventTypes;
            _otherStore = storage.GetEventStore((ConceptsEventStoreName)$"other-{Guid.NewGuid():N}").EventTypes;
            EventTypeId = $"ResolvedEvent-{Guid.NewGuid():N}";

            await _eventTypes.Register(new EventTypeDefinition(
                EventTypeId,
                EventTypeOwner.Client,
                false,
                [
                    new EventTypeGenerationDefinition(1, Schema("first")),
                    new EventTypeGenerationDefinition(2, Schema("second"))
                ],
                [new EventTypeMigrationDefinition(1, 2, [], new JsonObject { ["value"] = "up" }, new JsonObject { ["value"] = "down" })]));
        }

        async Task Because()
        {
            Latest = await _eventTypes.GetFor(EventTypeId);
            FirstGeneration = await _eventTypes.GetFor(EventTypeId, 1);
            SecondGeneration = await _eventTypes.GetFor(EventTypeId, 2);
            Definition = await _eventTypes.GetDefinition(EventTypeId);
            MigrationCount = Definition.Migrations.Count();
            HasFirst = await _eventTypes.HasFor(EventTypeId, 1);
            HasSecond = await _eventTypes.HasFor(EventTypeId, 2);
            HasUnknownGeneration = await _eventTypes.HasFor(EventTypeId, 99);
            HasUnknownType = await _eventTypes.HasFor($"never-registered-{Guid.NewGuid():N}");
            OtherStoreHasIt = await _otherStore.HasFor(EventTypeId);
            LatestForAll = await _eventTypes.GetLatestForAllEventTypes();
            ByIds = await _eventTypes.GetFor([EventTypeId]);

            try
            {
                await _eventTypes.GetFor($"never-registered-{Guid.NewGuid():N}");
            }
            catch (Exception ex)
            {
                UnknownTypeFailure = ex;
            }

            UnchangedRegistrationMutated = await _eventTypes.Register(
                new EventType(EventTypeId, 2),
                Schema("second"));
            ChangedRegistrationMutated = await _eventTypes.Register(
                new EventType(EventTypeId, 2),
                ChangedSchema());
            AfterChange = await _eventTypes.GetFor(EventTypeId, 2);

            await _eventTypes.Register(new EventType(EventTypeId, 2, true), ChangedSchema());
            AfterTombstone = await _eventTypes.GetFor(EventTypeId, 2);
        }

        static JsonSchema Schema(string description) => new() { Description = description };

        static JsonSchema ChangedSchema() => JsonSchema.FromJson("""{"type":"object","description":"second","properties":{"added":{"type":"string"}}}""");
    }

    [Fact] void should_resolve_the_first_generation_when_none_is_given() => Context.Latest.Type.Generation.Value.ShouldEqual(1u);
    [Fact] void should_resolve_the_schema_of_the_first_generation() => Context.FirstGeneration.Schema.Description.ShouldEqual("first");
    [Fact] void should_resolve_the_schema_of_the_second_generation() => Context.SecondGeneration.Schema.Description.ShouldEqual("second");
    [Fact] void should_keep_the_migration_between_the_generations() => Context.MigrationCount.ShouldEqual(1);
    [Fact] void should_know_both_generations() => (Context.HasFirst && Context.HasSecond).ShouldBeTrue();
    [Fact] void should_not_know_a_generation_that_was_never_registered() => Context.HasUnknownGeneration.ShouldBeFalse();
    [Fact] void should_not_know_a_type_that_was_never_registered() => Context.HasUnknownType.ShouldBeFalse();
    [Fact] void should_fail_to_resolve_a_type_that_was_never_registered() => Context.UnknownTypeFailure.ShouldBeOfExactType<MissingEventSchemaForEventType>();
    [Fact] void should_not_leak_into_another_event_store() => Context.OtherStoreHasIt.ShouldBeFalse();
    [Fact] void should_list_the_type_with_its_latest_generation() => Context.LatestForAll.Single(_ => _.Type.Id == Context.EventTypeId).Type.Generation.Value.ShouldEqual(2u);
    [Fact] void should_resolve_the_first_generation_when_asked_by_identifier_only() => Context.ByIds.Single().Type.Generation.Value.ShouldEqual(1u);
    [Fact] void should_not_report_a_mutation_for_an_unchanged_registration() => Context.UnchangedRegistrationMutated.ShouldBeFalse();
    [Fact] void should_report_a_mutation_for_a_changed_schema() => Context.ChangedRegistrationMutated.ShouldBeTrue();
    [Fact] void should_resolve_the_changed_schema() => Context.AfterChange.Schema.Properties.ContainsKey("added").ShouldBeTrue();
    [Fact] void should_resolve_the_tombstone_flag() => Context.AfterTombstone.Type.Tombstone.ShouldBeTrue();
}
