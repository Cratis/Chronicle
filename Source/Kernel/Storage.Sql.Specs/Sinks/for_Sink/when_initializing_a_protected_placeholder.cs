// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink;

public class when_initializing_a_protected_placeholder : Specification
{
    SqlSinkHarness _harness;
    ISink _sink;
    IProjection _projection;
    ReadModelsCompliance _compliance;
    IDictionary<string, object?> _placeholder;
    IDictionary<string, object?> _initialized;
    readonly Key _key = new("shelf", ArrayIndexers.NoIndexers);

    void Establish()
    {
        _harness = new SqlSinkHarness();
        var schema = JsonSchema.FromJson("""
            {"type":"object","properties":{
              "id":{"type":"string"},
              "capacity":{"type":"integer"},
              "secret":{"type":"string","compliance":[{"metadataType":"PII","details":""}]},
              "books":{"type":"array","items":{"type":"object","properties":{"isbn":{"type":"string"}}}}
            }}
            """);
        var definition = new ReadModelDefinition("shelf", "shelves", "Shelf", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "shelf-projection", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []);
        _sink = _harness.CreateSink(definition);
        var encryption = new Encryption();
        var keys = new InMemoryEncryptionKeyStorage();
        var handler = new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption);
        _compliance = new ReadModelsCompliance(new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance), new ExpandoObjectConverter(new TypeFormats()));
        _projection = Substitute.For<IProjection>();
        _projection.TargetReadModelSchema.Returns(schema);
        dynamic initial = new ExpandoObject();
        initial.capacity = 42;
        _projection.InitialModelState.Returns((ExpandoObject)initial);
        _projection.ChildrenPropertyPath.Returns(PropertyPath.Root);
        _projection.Accepts(Arg.Any<EventType>()).Returns(true);
        _projection.When(projection => projection.OnNext(Arg.Any<ProjectionEventContext>())).Do(call =>
        {
            var context = call.Arg<ProjectionEventContext>();
            if (context.ChildrenAffected)
            {
                dynamic book = new ExpandoObject();
                book.isbn = "978-1";
                context.Changeset.AddChild("books", (ExpandoObject)book);
            }
        });
    }

    async Task Because()
    {
        await Apply(ProjectionOperationType.ChildrenAffected, 0UL);
        _placeholder = (await _harness.ReadStoredRows()).Single();
        await Apply(ProjectionOperationType.From, 1UL);
        _initialized = (await _sink.FindOrDefault(_key))!;
    }

    void Destroy() => _harness.Dispose();

    [Fact] void should_not_synthesize_a_zero_capacity_on_the_placeholder() => _placeholder["capacity"].ShouldBeNull();
    [Fact] void should_keep_the_placeholder_uninitialized() => _placeholder[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(false);
    [Fact] void should_apply_the_configured_capacity() => _initialized["capacity"].ShouldEqual(42);
    [Fact] void should_keep_the_child() => ((IEnumerable<object>)_initialized["books"]!).Count().ShouldEqual(1);
    [Fact] void should_initialize_the_root() => _initialized[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(true);

    async Task Apply(ProjectionOperationType operation, EventSequenceNumber sequenceNumber)
    {
        var @event = new AppendedEvent(EventContext.From("store", "namespace", EventType.Unknown, EventSourceType.Default, "shelf", EventStreamType.All, EventStreamId.Default, sequenceNumber, CorrelationId.NotSet), new ExpandoObject());
        var context = new ProjectionEventContext(_key, @event, new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), @event, new ExpandoObject()), operation, false);
        context = await new SetInitialState(_sink, NullLogger<SetInitialState>.Instance).Perform(_projection, context);
        context = await new DecryptInitialState(_compliance, "store", "namespace").Perform(_projection, context);
        context = await new HandleEvent(Substitute.For<IEventSequenceStorage>(), _sink, NullLogger<HandleEvent>.Instance).Perform(_projection, context);
        context = await new EncryptChangeset(_compliance, new ObjectComparer(), "store", "namespace").Perform(_projection, context);
        (await _sink.ApplyChanges(_key, context.Changeset, sequenceNumber)).ShouldBeEmpty();
    }
}
