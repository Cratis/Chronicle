// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_an_older_generation_inherits_personal_data : given.a_replay
{
    void Establish()
    {
        var own = JsonSchema.FromJson("""{"type":"object","properties":{"email":{"type":"string"}}}""");
        var newer = JsonSchema.FromJson("""{"type":"object","properties":{"email":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}""");
        _types.Configure().GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>()).Returns(new EventTypeSchema(new("Claimed", 1), EventTypeOwner.None, EventTypeSource.Code, own));
        _types.GetDefinition(Arg.Any<EventTypeId>()).Returns(new EventTypeDefinition("Claimed", EventTypeOwner.Client, false, [new(EventTypeGeneration.First, own), new(new EventTypeGeneration(2), newer)], []));
        var eventStore = Substitute.For<IEventStoreStorage>();
        eventStore.EventTypes.Returns(_types);
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        _content = new(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _types, _metadata, _converter, new EventTypeSchemaCache(storage));

        var plaintext = new ExpandoObject();
        ((IDictionary<string, object?>)plaintext)["email"] = "plaintext";
        _converter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(new JsonObject { ["email"] = "ciphertext" });

        // Only the effective schema, which inherits the later generation's PII classification, releases the plaintext.
        _metadata.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Is<JsonSchema>(schema => ConstraintPersonalData.PropertiesOf(schema).Contains("email")), Arg.Any<string>(), Arg.Any<JsonObject>()).Returns(new JsonObject { ["email"] = "plaintext" });
        _converter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>()).Returns(plaintext);
        Event(0, "source", "Claimed", "ciphertext", generation: 1);
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] async Task should_claim_the_hash_of_the_released_value() => (await Values("source")).ShouldContainOnly(Hash("plaintext").Value);
    [Fact] async Task should_not_claim_the_hash_of_the_ciphertext() => (await Values("source")).ShouldNotContain(Hash("ciphertext").Value);
    [Fact] async Task should_not_claim_the_plaintext() => (await Values("source")).ShouldNotContain("plaintext");
    [Fact] void should_record_the_subject_of_the_inherited_personal_data() => _unique.Received(1).Save(Arg.Any<EventSourceId>(), _definition, Arg.Any<EventSequenceNumber>(), Hash("plaintext"), Arg.Any<string>(), Arg.Is<Subject?>(subject => subject != null && subject.Value == "source"), Arg.Any<CancellationToken>());
}
