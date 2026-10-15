// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.InMemory.EventSequences;
using Cratis.Chronicle.Storage.InMemory.Identities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.given;

public class a_migrated_event_with_dynamic_and_nested_protected_values : a_stored_event
{
    JsonSchema _first;
    JsonSchema _second;

    async Task Establish()
    {
        var encryption = new Encryption();
        var key = encryption.GenerateKey();
        var keys = Substitute.For<IEncryptionKeyStorage>();
        keys.TryGetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).Returns(key);
        var provisioner = Substitute.For<IManagedEncryptionKeyProvisioner>();
        provisioner.EnsureKeyFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).Returns(key);
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(provisioner, keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);
        _first = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"contacts":{"type":"object","additionalProperties":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}},
             "groups":{"type":"array","items":{"type":"array","items":{"type":"string"},"compliance":[{"metadataType":"PII","details":""}]}}}}
            """);
        _second = await JsonSchema.FromJsonAsync(_first.ToJson());
        _second.Properties["detail"] = new JsonSchemaProperty("detail", new JsonObject(), _second) { Type = JsonObjectType.String };
        _command = _command with { Content = """{"contacts":{"home":"001","work":"private@example.com"},"groups":[["001","private@example.com"],[]]}""" };
    }

    protected async Task Store(string upcast)
    {
        var eventTypes = _storage.GetEventStore("store").EventTypes;
        eventTypes.HasFor("event", 2U).Returns(true);
        eventTypes.GetFor("event", 1U).Returns(new EventTypeSchema(new("event", 1), EventTypeOwner.Client, EventTypeSource.Code, _first));
        eventTypes.GetFor("event", 2U).Returns(new EventTypeSchema(new("event", 2), EventTypeOwner.Client, EventTypeSource.Code, _second));
        eventTypes.Configure().GetDefinition("event").Returns(new EventTypeDefinition("event", EventTypeOwner.Client, false, [new(1, _first), new(2, _second)], [new(1, 2, [], JsonNode.Parse(upcast)!.AsObject(), new JsonObject())]));
        var plaintext = JsonNode.Parse(_command.Content)!.AsObject();
        var protectedSource = await _manager.Apply("store", "tenant", _first, "source", plaintext);
        var migrations = new ProtectedEventTypeMigrations(eventTypes, new EventTypeMigrations(_storage, _converter), _manager, _converter);
        var generations = await migrations.MigratePlaintext("store", "tenant", new("event", 1), plaintext, _converter.ToExpandoObject(protectedSource, _first), "source");
        var sequence = new EventSequenceStorage("store", "tenant", "log", new IdentityStorage());
        var appended = await sequence.Append(EventSequenceNumber.First, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, new("event", 1), CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, generations, new Dictionary<EventTypeGeneration, EventHash>());
        appended.IsSuccess.ShouldBeTrue();
        using var cursor = await sequence.GetRange(EventSequenceNumber.First, EventSequenceNumber.First);
        (await cursor.MoveNext()).ShouldBeTrue();
        _stored = cursor.Current.Single();
    }
}
