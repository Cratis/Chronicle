// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence_with_inherited_protection : an_event_sequence
{
    protected const string SubjectIdentifier = "inherited-protection-subject";
    protected readonly List<EventToAppendToStorage> _storedEvents = [];
    protected readonly List<JsonObject> _plaintextInputs = [];
    protected EventTypeDefinition _definition;
    protected JsonSchemaMetadataManager _metadataManager;
    protected ExpandoObjectConverter _converter;
    protected InMemoryEncryptionKeyStorage _keys;
    protected EventHashCalculator _hashCalculator;

    async Task Establish()
    {
        var sourceSchema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"name":{"type":"string"}}}""");
        var targetSchema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}""");
        _definition = new EventTypeDefinition(
            _eventType.Id,
            EventTypeOwner.Client,
            false,
            [new(1, sourceSchema), new(2, targetSchema)],
            [new EventTypeMigrationDefinition(1, 2, [], new JsonObject(), new JsonObject())]);
        _eventTypesStorage.GetDefinition(_eventType.Id).Returns(_definition);
        _eventTypesStorage.GetFor(_eventType.Id, _eventType.Generation)
            .Returns(new EventTypeSchema(_eventType, EventTypeOwner.Client, EventTypeSource.Code, sourceSchema));

        _keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(_keys, encryption);
        _metadataManager = new JsonSchemaMetadataManager(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(provisioner, _keys, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        _converter = new ExpandoObjectConverter(new TypeFormats());
        _hashCalculator = new EventHashCalculator();
        _complianceManager.Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(call => _metadataManager.Apply(call.ArgAt<EventStoreName>(0), call.ArgAt<EventStoreNamespaceName>(1), call.ArgAt<JsonSchema>(2), call.ArgAt<string>(3), call.ArgAt<JsonObject>(4)));
        _complianceManager.ApplyReportingKeys(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(call => _metadataManager.ApplyReportingKeys(call.ArgAt<EventStoreName>(0), call.ArgAt<EventStoreNamespaceName>(1), call.ArgAt<JsonSchema>(2), call.ArgAt<string>(3), call.ArgAt<JsonObject>(4)));
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>())
            .Returns(call => _converter.ToExpandoObject(call.ArgAt<JsonObject>(0), call.ArgAt<JsonSchema>(1)));
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(call => _converter.ToJsonObject(call.ArgAt<ExpandoObject>(0), call.ArgAt<JsonSchema>(1)));
        _eventHashCalculator.Calculate(Arg.Any<EventTypeId>(), Arg.Any<EventSourceId>(), Arg.Any<ExpandoObject>())
            .Returns(call => _hashCalculator.Calculate(call.ArgAt<EventTypeId>(0), call.ArgAt<EventSourceId>(1), call.ArgAt<ExpandoObject>(2)));
        var migrations = new EventTypeMigrations(_storage, _converter);
        _eventTypeMigrations.MigrateToAllGenerations(
                Arg.Any<EventStoreName>(),
                Arg.Any<EventType>(),
                Arg.Any<JsonObject>(),
                Arg.Any<ExpandoObject>(),
                Arg.Any<Action<JsonObject, JsonSchema, ExpandoObject>>(),
                Arg.Any<Action<JsonObject, JsonObject>>())
            .Returns(call => migrations.MigrateToAllGenerations(call.ArgAt<EventStoreName>(0), call.ArgAt<EventType>(1), call.ArgAt<JsonObject>(2), call.ArgAt<ExpandoObject>(3)));
        _eventSequenceStorage.Append(
                Arg.Any<EventSequenceNumber>(),
                Arg.Any<EventSourceType>(),
                Arg.Any<EventSourceId>(),
                Arg.Any<EventStreamType>(),
                Arg.Any<EventStreamId>(),
                Arg.Any<EventType>(),
                Arg.Any<CorrelationId>(),
                Arg.Any<IEnumerable<Causation>>(),
                Arg.Any<IEnumerable<IdentityId>>(),
                Arg.Any<IEnumerable<Tag>>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<IDictionary<EventTypeGeneration, ExpandoObject>>(),
                Arg.Any<IDictionary<EventTypeGeneration, EventHash>>(),
                Arg.Any<Subject?>())
            .Returns(CaptureSingle);
        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>())
            .Returns(call => Task.FromResult<Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>>(
                call.Arg<IEnumerable<EventToAppendToStorage>>().Select(Capture).ToArray()));
    }

    protected JsonObject Plaintext(string marker)
    {
        var content = new JsonObject { ["name"] = marker };
        _plaintextInputs.Add(content);
        return content;
    }

    protected async Task AssertProtectedOnce()
    {
        _storedEvents.Count.ShouldEqual(_plaintextInputs.Count);
        foreach (var stored in _storedEvents)
        {
            stored.GenerationalContent.Count.ShouldEqual(2);
            var expected = _plaintextInputs[_storedEvents.IndexOf(stored)]["name"]!.GetValue<string>();
            ((IDictionary<string, object?>)stored.Content)["name"].ShouldNotEqual(expected);
            foreach (var (generation, content) in stored.GenerationalContent)
            {
                var ciphertext = ((IDictionary<string, object?>)content)["name"];
                ciphertext.ShouldNotEqual(expected);
                var json = new JsonObject { ["name"] = (string)ciphertext! };
                var released = await _metadataManager.ReleaseStrict(
                    EventStore,
                    EventStoreNamespace,
                    GenerationProtectionSchemas.For(_definition, generation),
                    SubjectIdentifier,
                    json);
                released["name"]!.GetValue<string>().ShouldEqual(expected);
            }
        }
    }

    protected void AssertHashes()
    {
        _storedEvents.Count.ShouldEqual(_plaintextInputs.Count);
        foreach (var stored in _storedEvents)
        {
            stored.Hash.ShouldEqual(_hashCalculator.Calculate(_eventType.Id, _eventSourceId, stored.Content));
            stored.ContentHashes.Count.ShouldEqual(2);
            foreach (var (generation, content) in stored.GenerationalContent)
            {
                stored.ContentHashes[generation].ShouldEqual(_hashCalculator.Calculate(_eventType.Id, _eventSourceId, content));
            }
        }
    }

    protected async Task AssertErasure()
    {
        var identifier = new EncryptionKeyIdentifier(SubjectIdentifier);
        (await _keys.HasFor(EventStore, EventStoreNamespace, identifier)).ShouldBeTrue();
        (await _keys.HasFor(EventStore, EventStoreNamespace, new EncryptionKeyIdentifier(_eventSourceId.Value))).ShouldBeFalse();
        await _keys.RecordErasureFor(EventStore, EventStoreNamespace, identifier);
        await _keys.DeleteFor(EventStore, EventStoreNamespace, identifier);
        foreach (var stored in _storedEvents)
        {
            foreach (var (generation, content) in stored.GenerationalContent)
            {
                var json = new JsonObject { ["name"] = (string)((IDictionary<string, object?>)content)["name"]! };
                var released = await _metadataManager.ReleaseStrict(
                    EventStore,
                    EventStoreNamespace,
                    GenerationProtectionSchemas.For(_definition, generation),
                    SubjectIdentifier,
                    json);
                released["name"]!.GetValue<string>().ShouldEqual(string.Empty);
            }
        }
    }

    Task<Result<AppendedEvent, DuplicateEventSequenceNumber>> CaptureSingle(NSubstitute.Core.CallInfo call)
    {
        var content = call.ArgAt<IDictionary<EventTypeGeneration, ExpandoObject>>(11);
        var hashes = call.ArgAt<IDictionary<EventTypeGeneration, EventHash>>(12);
        var stored = new EventToAppendToStorage(
            call.ArgAt<EventSequenceNumber>(0),
            call.ArgAt<EventSourceType>(1),
            call.ArgAt<EventSourceId>(2),
            call.ArgAt<EventStreamType>(3),
            call.ArgAt<EventStreamId>(4),
            call.ArgAt<EventType>(5),
            call.ArgAt<CorrelationId>(6),
            call.ArgAt<IEnumerable<Causation>>(7),
            call.ArgAt<IEnumerable<IdentityId>>(8),
            call.ArgAt<IEnumerable<Tag>>(9),
            call.ArgAt<DateTimeOffset>(10),
            content[_eventType.Generation],
            hashes[_eventType.Generation],
            call.ArgAt<Subject?>(13))
        {
            GenerationalContent = content,
            ContentHashes = hashes
        };
        return Task.FromResult<Result<AppendedEvent, DuplicateEventSequenceNumber>>(Capture(stored));
    }

    AppendedEvent Capture(EventToAppendToStorage stored)
    {
        _storedEvents.Add(stored);
        return new AppendedEvent(EventContext.From(
                EventStore,
                EventStoreNamespace,
                stored.EventType,
                stored.EventSourceType,
                stored.EventSourceId,
                stored.EventStreamType,
                stored.EventStreamId,
                stored.SequenceNumber,
                stored.CorrelationId),
            stored.Content);
    }
}
