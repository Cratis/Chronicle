// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence_with_security_values : an_event_sequence
{
    protected JsonSchema _schema;
    protected JsonObject _plaintext;
    protected JsonSchemaMetadataManager _manager;
    protected ExpandoObjectConverter _converter;
    protected Storage.InMemory.EventSequences.EventSequenceStorage _memory;

    void Establish()
    {
        _schema = JsonSchema.FromJson(
            """
            {"type":"object","properties":{
                "subjectCount":{"type":"integer","format":"int32","security":[{"metadataType":"EncryptedSubject","details":""}]},
                "subjectFlag":{"type":"boolean","security":[{"metadataType":"EncryptedSubject","details":""}]},
                "namespaceCount":{"type":"integer","security":[{"metadataType":"EncryptedNamespace","details":""}]},
                "namespaceFlag":{"type":"boolean","security":[{"metadataType":"EncryptedNamespace","details":""}]},
                "globalCount":{"type":"integer","security":[{"metadataType":"EncryptedGlobal","details":""}]},
                "globalFlag":{"type":"boolean","security":[{"metadataType":"EncryptedGlobal","details":""}]}
            }}
            """);
        _plaintext = new JsonObject
        {
            ["subjectCount"] = 37,
            ["subjectFlag"] = true,
            ["namespaceCount"] = 38,
            ["namespaceFlag"] = false,
            ["globalCount"] = 39,
            ["globalFlag"] = true
        };
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(keys, encryption);
        _manager = new(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(
                new EncryptedSubjectValueHandler(provisioner, keys, encryption),
                new EncryptedNamespaceValueHandler(provisioner, keys, encryption),
                new EncryptedGlobalValueHandler(provisioner, keys, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        _converter = new(new TypeFormats());
        _memory = new(EventStore, EventStoreNamespace, SequenceId, _identityStorage);
        _eventTypesStorage.GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>())
            .Returns(new EventTypeSchema(_eventType, EventTypeOwner.Server, EventTypeSource.Code, _schema));
        _complianceManager.Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), _schema, Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(call => _manager.Apply(EventStore, EventStoreNamespace, _schema, call.Arg<string>(), call.Arg<JsonObject>()));
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), _schema)
            .Returns(call => _converter.ToExpandoObject(call.Arg<JsonObject>(), _schema));
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
            .Returns(call => _memory.Append(
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
                call.ArgAt<IDictionary<EventTypeGeneration, ExpandoObject>>(11),
                call.ArgAt<IDictionary<EventTypeGeneration, EventHash>>(12),
                call.ArgAt<Subject?>(13)));
    }
}
