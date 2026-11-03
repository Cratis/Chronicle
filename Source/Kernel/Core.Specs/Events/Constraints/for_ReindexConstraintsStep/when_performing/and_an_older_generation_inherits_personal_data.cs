// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Schemas;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_an_older_generation_inherits_personal_data : for_ReindexConstraintsStep.given.a_closing_constraint_reindex_step
{
    UniqueConstraintDefinition _unique;
    UniqueConstraintValue _released;
    UniqueConstraintValue _ciphertext;

    void Establish()
    {
        _unique = new("UniqueEmail", [new("Registered", ["email"])]);
        _definitions.Add(_unique);
        _state.Changes = [new(_unique.Name, true, [ConstraintChangeType.EventAdded])];
        _released = new List<UniqueConstraintPropertyAndValue> { new("email", "plaintext") }.GetValue();
        _ciphertext = new List<UniqueConstraintPropertyAndValue> { new("email", "ciphertext") }.GetValue();

        var own = JsonSchema.FromJson("""{"type":"object","properties":{"email":{"type":"string"}}}""");
        var newer = JsonSchema.FromJson("""{"type":"object","properties":{"email":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}""");
        _eventTypes.Configure().GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>()).Returns(new EventTypeSchema(new("Registered", 1), EventTypeOwner.Client, EventTypeSource.Code, own));
        _eventTypes.GetDefinition(Arg.Any<EventTypeId>()).Returns(new EventTypeDefinition("Registered", EventTypeOwner.Client, false, [new(EventTypeGeneration.First, own), new(new EventTypeGeneration(2), newer)], []));
        _schemaCache = new EventTypeSchemaCache(_storage);

        _converter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(new JsonObject { ["email"] = "ciphertext" });
        _metadata.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>()).Returns(new JsonObject { ["email"] = "plaintext" });
        _converter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>()).Returns(Payload("email", "plaintext"));
        _events.Add(Event("Registered", 0, Payload("email", "ciphertext")));
    }

    Task Because() => Perform();

    [Fact] void should_claim_the_hash_of_the_released_value() => _uniqueStorage.Received(1).Save(Arg.Any<EventSourceId>(), _unique, Arg.Any<EventSequenceNumber>(), _released, Arg.Any<string>(), Arg.Any<Subject?>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_claim_the_hash_of_the_ciphertext() => _uniqueStorage.DidNotReceive().Save(Arg.Any<EventSourceId>(), _unique, Arg.Any<EventSequenceNumber>(), _ciphertext, Arg.Any<string>(), Arg.Any<Subject?>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_claim_the_plaintext() => _uniqueStorage.DidNotReceive().Save(Arg.Any<EventSourceId>(), _unique, Arg.Any<EventSequenceNumber>(), new UniqueConstraintValue("plaintext"), Arg.Any<string>(), Arg.Any<Subject?>(), Arg.Any<CancellationToken>());
    [Fact] void should_record_the_subject_of_the_inherited_personal_data() => _uniqueStorage.Received(1).Save(Arg.Any<EventSourceId>(), _unique, Arg.Any<EventSequenceNumber>(), _released, Arg.Any<string>(), Arg.Is<Subject?>(subject => subject != null && subject.Value == "source"), Arg.Any<CancellationToken>());
}
