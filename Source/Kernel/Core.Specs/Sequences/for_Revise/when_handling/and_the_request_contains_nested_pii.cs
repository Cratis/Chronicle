// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Arc.Authorization;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Events.EventSequences;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Sequences.for_Revise.when_handling;

public class and_the_request_contains_nested_pii : Specification
{
    IGrainFactory _grainFactory;
    IStorage _storage;
    IEventSequence _systemSequence;
    IEventSequence _targetSequence;
    JsonSchemaMetadataManager _manager;
    InMemoryEncryptionKeyStorage _keys;
    Concepts.Events.EventContext _context;
    JsonSchema _schema;
    EventRevised _request;
    protected JsonObject _released;
    protected JsonObject _erased;
    protected Exception? _errorAfterErasure;

    protected virtual Subject? OriginalSubject => (Subject)"owner-not-source";
    protected string Identifier => OriginalSubject?.IsSet == true ? OriginalSubject.Value : "source";

    async Task Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();
        _systemSequence = Substitute.For<IEventSequence>();
        _targetSequence = Substitute.For<IEventSequence>();
        _grainFactory.GetGrain<IEventSequence>(new EventSequenceKey(EventSequenceId.System, "store", "namespace").ToString(), null).Returns(_systemSequence);
        _grainFactory.GetGrain<IEventSequence>(new EventSequenceKey(EventSequenceId.Log, "store", "namespace").ToString(), null).Returns(_targetSequence);
        _keys = new();
        var encryption = new Encryption();
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(_keys, encryption), _keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"profile":{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}}}""");
        var eventType = new Concepts.Events.EventType("event", 2);
        _context = Concepts.Events.EventContext.From("store", "namespace", eventType, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, 0UL, CorrelationId.New(), subject: OriginalSubject);
        _storage.GetEventStore("store").EventTypes.GetFor(eventType.Id, eventType.Generation).Returns(new EventTypeSchema(eventType, EventTypeOwner.Server, EventTypeSource.Code, _schema));
        _storage.GetEventStore("store").GetNamespace("namespace").GetEventSequence(EventSequenceId.Log).GetEventAt(0UL).Returns(new Concepts.Events.AppendedEvent(_context, new ExpandoObject()));
    }

    async Task Because()
    {
        await new Revise("store", "namespace", EventSequenceId.Log, 0, new EventType("event", 2, false), """{"profile":{"name":"revised name"}}""", CausedBy: new Identity("system", "system", "system", null))
            .Handle(_grainFactory, new RequestCausation(new HttpContextAccessor()), Substitute.For<ICurrentPrincipalAccessor>(), _storage, _manager);
        _request = (EventRevised)_systemSequence.ReceivedCalls().Single(call => call.GetMethodInfo().Name == nameof(IEventSequence.Append)).GetArguments()[1]!;
        await new EventSequencesReactor(_grainFactory, new JsonSerializerOptions(), NullLogger<EventSequencesReactor>.Instance, _storage, _manager).Revised(_request, _context);
        _released = (JsonObject)_targetSequence.ReceivedCalls().Single(call => call.GetMethodInfo().Name == nameof(IEventSequence.Revise)).GetArguments()[2]!;
        await _keys.RecordErasureFor("store", "namespace", Identifier);
        await _keys.DeleteFor("store", "namespace", Identifier);
        _erased = await _manager.Release("store", "namespace", _schema, Identifier, JsonNode.Parse(_request.Content)!.AsObject());
        _errorAfterErasure = await Catch.Exception(() => new Revise("store", "namespace", EventSequenceId.Log, 0, new EventType("event", 2, false), """{"profile":{"name":"new personal value"}}""", CausedBy: new Identity("system", "system", "system", null))
            .Handle(_grainFactory, new RequestCausation(new HttpContextAccessor()), Substitute.For<ICurrentPrincipalAccessor>(), _storage, _manager));
    }

    [Fact] void should_not_persist_personal_plaintext_in_the_request() => _request.Content.Contains("revised name", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_release_the_request_before_the_revision_protects_it_again() => _released["profile"]!["name"]!.GetValue<string>().ShouldEqual("revised name");
    [Fact] void should_not_reveal_the_request_after_erasure() => _erased["profile"]!["name"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_reject_new_personal_content_after_erasure() => _errorAfterErasure.ShouldBeOfExactType<SchemaMetadataActionFailed>();
    [Fact] void should_not_append_a_request_for_an_erased_subject() => _systemSequence.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IEventSequence.Append)).ShouldEqual(1);
}
