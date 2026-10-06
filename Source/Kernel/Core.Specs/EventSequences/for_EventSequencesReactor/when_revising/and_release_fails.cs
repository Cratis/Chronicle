// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Events.EventSequences;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesReactor.when_revising;

public class and_release_fails : Specification
{
    EventSequencesReactor _reactor;
    protected IEventSequence _sequence;
    protected IEventSequenceStorage _sequenceStorage;
    EventRevised _request;
    EventContext _context;
    protected Exception _error;
    protected JsonObject _original;
    protected string _originalText;

    protected virtual bool UsesObjectArray => false;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync(UsesObjectArray
            ? """{"type":"object","properties":{"contacts":{"type":"array","items":{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}}}}"""
            : """{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}""");
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(keys, encryption);
        var manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(provisioner, keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);
        _original = await manager.Apply("store", "namespace", schema, "owner", JsonNode.Parse(UsesObjectArray ? """{"contacts":[{"name":"original"}]}""" : """{"name":"original"}""")!.AsObject());
        _originalText = _original.ToJsonString();
        var revision = await manager.Apply("store", "namespace", schema, "owner", JsonNode.Parse(UsesObjectArray ? """{"contacts":[{"name":"revised"}]}""" : """{"name":"revised"}""")!.AsObject());

        // Missing key material without a recorded erasure is an operational failure, not erasure.
        await keys.DeleteFor("store", "namespace", "owner");
        var type = new EventType("personal-event", 1);
        _context = EventContext.From("store", "namespace", type, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, 1UL, CorrelationId.NotSet, subject: "owner");
        _request = new EventRevised(Concepts.EventSequences.WellKnownEventSequences.EventLog, 1UL, type, revision.ToJsonString());
        var factory = Substitute.For<IGrainFactory>();
        _sequence = Substitute.For<IEventSequence>();
        factory.GetGrain<IEventSequence>(Arg.Any<string>(), Arg.Any<string?>()).Returns(_sequence);
        _sequenceStorage = Substitute.For<IEventSequenceStorage>();
        _sequenceStorage.GetEventAt(1UL).Returns(new AppendedEvent(_context, new ExpandoObject()));
        var storage = Substitute.For<IStorage>();
        var store = Substitute.For<IEventStoreStorage>();
        var ns = Substitute.For<IEventStoreNamespaceStorage>();
        var types = Substitute.For<IEventTypesStorage>();
        storage.GetEventStore(_context.EventStore).Returns(store);
        store.GetNamespace(_context.Namespace).Returns(ns);
        store.EventTypes.Returns(types);
        ns.GetEventSequence(_request.Sequence).Returns(_sequenceStorage);
        types.GetFor(type.Id, type.Generation).Returns(new EventTypeSchema(type, EventTypeOwner.Server, EventTypeSource.Code, schema));
        _reactor = new EventSequencesReactor(factory, new JsonSerializerOptions(), NullLogger<EventSequencesReactor>.Instance, storage, manager);
    }

    async Task Because() => _error = await Catch.Exception(() => _reactor.Revised(_request, _context));

    [Fact] void should_fail_instead_of_revising_with_blanks() => _error.ShouldBeOfExactType<SchemaMetadataActionFailed>();
    [Fact] void should_not_call_the_revision_grain() => _sequence.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "Revise").ShouldBeFalse();
    [Fact] void should_not_write_a_revision() => _sequenceStorage.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "Revise").ShouldBeFalse();
    [Fact] void should_leave_existing_content_unchanged() => _original.ToJsonString().ShouldEqual(_originalText);
}
