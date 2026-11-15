// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events.EventSequences;
using Cratis.Chronicle.Events.for_EventCompliance.given;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesReactor.when_revising;

[PerFactSpecificationLifecycle]
public class and_the_request_generation_inherits_protection : a_stored_generation_with_inherited_protection
{
    EventSequencesReactor _reactor;
    IEventSequence _sequence;
    EventRevised _request;
    EventContext _context;
    JsonObject _released;

    async Task Establish()
    {
        var effective = await _cache.GetProtectionSchemaFor("store", _type.Id, _type.Generation, _own);
        var content = await _manager.Apply("store", "namespace", effective, Identifier, _plaintext);
        _context = EventWith(_plaintext).Context with { Subject = "request-subject-is-not-owner" };
        _request = new(Concepts.EventSequences.WellKnownEventSequences.EventLog, 0UL, _type, content.ToJsonString());
        var factory = Substitute.For<IGrainFactory>();
        _sequence = Substitute.For<IEventSequence>();
        factory.GetGrain<IEventSequence>(Arg.Any<string>(), Arg.Any<string?>()).Returns(_sequence);
        var sequenceStorage = Substitute.For<IEventSequenceStorage>();
        sequenceStorage.GetEventAt(0UL).Returns(EventWith(_plaintext));
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore("store").EventTypes.Returns(_eventTypes);
        storage.GetEventStore("store").GetNamespace("namespace").GetEventSequence(_request.Sequence).Returns(sequenceStorage);
        _reactor = new(factory, new JsonSerializerOptions(), NullLogger<EventSequencesReactor>.Instance, storage, _manager);
    }

    async Task Because()
    {
        await _reactor.Revised(_request, _context);
        _released = (JsonObject)_sequence.ReceivedCalls().Single(call => call.GetMethodInfo().Name == nameof(IEventSequence.Revise)).GetArguments()[2]!;
    }

    [Fact] void should_release_inherited_request_protection_exactly_once_for_the_original_subject() => JsonNode.DeepEquals(_released, _plaintext).ShouldBeTrue();
    [Fact] void should_not_put_personal_or_confidential_plaintext_in_the_request() => _request.Content.ShouldNotContain("historical");

    [Fact]
    async Task should_fail_closed_if_request_key_material_disappears()
    {
        _sequence.ClearReceivedCalls();
        await _keys.DeleteFor("store", "namespace", Identifier);
        var error = await Catch.Exception(() => _reactor.Revised(_request, _context));
        error.ShouldBeOfExactType<SchemaMetadataActionFailed>();
        _sequence.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(IEventSequence.Revise)).ShouldBeFalse();
    }
}
