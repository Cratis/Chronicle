// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_a_declaration_contains_a_nested_join : Specification
{
    IProjection _projection;
    Joined _joined;
    ResolvedJoin _backfilled;

    async Task Because()
    {
        const string declaration = """
            projection Test => TestModel
              no automap
              from Created
              nested details
                from Attached
                  reference = reference
                join Linked on reference
                  with Joined
                    value = value
            """;
        var language = new LanguageService(new Generator(), new Cratis.Types.KnownInstancesOf<DeclarationLanguage.CodeGeneration.IProjectionCodeGenerator>());
        var result = language.Compile(declaration, ProjectionOwner.Client, [], []);
        var definition = result.Match(_ => _, errors => throw new InvalidOperationException(string.Join(", ", errors.Errors)));
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"string"},"details":{"type":"object","properties":{"reference":{"type":"string"},"value":{"type":"string"}}}}}
            """);
        var readModel = new ReadModelDefinition(
            "TestModel",
            "TestModel",
            "TestModel",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema },
            []);
        EventStoreName eventStore = "event-store";
        EventStoreNamespaceName @namespace = "namespace";
        var storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        var sequence = Substitute.For<IEventSequenceStorage>();
        namespaceStorage.GetEventSequence(EventSequenceId.Log).Returns(sequence);
        eventStoreStorage.GetNamespace(@namespace).Returns(namespaceStorage);
        storage.GetEventStore(eventStore).Returns(eventStoreStorage);
        var formats = new TypeFormats();
        var keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var valueResolvers = new EventValueProviderExpressionResolvers(formats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        var factory = new ProjectionFactory(
            new ReadModelPropertyExpressionResolvers(valueResolvers, formats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance),
            valueResolvers,
            new KeyExpressionResolvers(valueResolvers, keyResolvers, NullLogger<KeyExpressionResolvers>.Instance),
            new ExpandoObjectConverter(formats),
            keyResolvers,
            storage,
            NullLogger<ProjectionFactory>.Instance);
        _projection = await factory.Create(eventStore, @namespace, definition, readModel, []);
        var incoming = new AppendedEvent(
            new(
                (EventType)"Joined",
                EventSourceType.Default,
                "company",
                EventStreamType.All,
                EventStreamId.Default,
                0,
                DateTimeOffset.UtcNow,
                "123b8935-a1a4-410d-aace-e340d48f0aa0",
                "41f18595-4748-4b01-88f7-4c0d0907aa90",
                CorrelationId.New(),
                [],
                Identity.System,
                [],
                EventHash.NotSet),
            new ExpandoObject());
        ((IDictionary<string, object?>)incoming.Content)["value"] = "joined value";
        var details = new ExpandoObject();
        ((IDictionary<string, object?>)details)["reference"] = "company";
        var initialState = new ExpandoObject();
        ((IDictionary<string, object?>)initialState)["details"] = details;
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), incoming, initialState);
        _projection.OnNext(new ProjectionEventContext(new Key("root", ArrayIndexers.NoIndexers), incoming, changeset, ProjectionOperationType.Join, false, "company"));
        _joined = changeset.Changes.OfType<Joined>().Single();

        sequence.TryGetLastEventBefore(Arg.Any<EventTypeId>(), "company", Arg.Any<EventSequenceNumber>())
            .Returns(Task.FromResult(Catch<Option<AppendedEvent>>.Success(new Option<AppendedEvent>(incoming))));
        var attachedContent = new ExpandoObject();
        ((IDictionary<string, object?>)attachedContent)["reference"] = "company";
        var attached = new AppendedEvent(incoming.Context with { EventType = (EventType)"Attached", SequenceNumber = 2 }, attachedContent);
        var backfillState = new ExpandoObject();
        ((IDictionary<string, object?>)backfillState)["details"] = new ExpandoObject();
        var backfillChangeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), attached, backfillState);
        _projection.OnNext(new ProjectionEventContext(new Key("root", ArrayIndexers.NoIndexers), attached, backfillChangeset, ProjectionOperationType.From, false));
        _backfilled = backfillChangeset.Changes.OfType<ResolvedJoin>().Single();
    }

    [Fact] void should_subscribe_to_the_nested_join_event() => _projection.EventTypes.ShouldContain((EventType)"Joined");
    [Fact] void should_wire_a_join_subscription_and_backfill() => ((Projection)_projection).Subscriptions.Count.ShouldEqual(4);
    [Fact] void should_join_on_the_nested_property() => _joined.OnProperty.Path.ShouldEqual("details.reference");
    [Fact] void should_backfill_on_the_nested_property() => _backfilled.OnProperty.Path.ShouldEqual("details.reference");
    [Fact] void should_backfill_the_nested_read_model()
    {
        var changed = _backfilled.Changes.OfType<PropertiesChanged<ExpandoObject>>().Single();
        var details = (IDictionary<string, object?>)((IDictionary<string, object?>)changed.State)["details"]!;
        details["value"].ShouldEqual("joined value");
    }
    [Fact] void should_change_the_nested_read_model()
    {
        var changed = _joined.Changes.OfType<PropertiesChanged<ExpandoObject>>().Single();
        var details = (IDictionary<string, object?>)((IDictionary<string, object?>)changed.State)["details"]!;
        details["value"].ShouldEqual("joined value");
    }
}
