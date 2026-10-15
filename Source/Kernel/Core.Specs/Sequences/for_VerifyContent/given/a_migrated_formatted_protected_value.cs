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

public class a_migrated_formatted_protected_value : a_stored_event
{
    void Establish()
    {
        var encryption = new Encryption();
        var key = encryption.GenerateKey();
        var keys = Substitute.For<IEncryptionKeyStorage>();
        keys.TryGetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).Returns(key);
        var provisioner = Substitute.For<IManagedEncryptionKeyProvisioner>();
        provisioner.EnsureKeyFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).Returns(key);
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(provisioner, keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);
    }

    protected async Task Store(string targetSchema, string content, int attemptedGeneration = 1)
    {
        var first = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}""");
        var second = await JsonSchema.FromJsonAsync(targetSchema);
        var eventTypes = _storage.GetEventStore("store").EventTypes;
        eventTypes.HasFor("event", 2U).Returns(true);
        eventTypes.GetFor("event", 1U).Returns(new EventTypeSchema(new("event", 1), EventTypeOwner.Client, EventTypeSource.Code, first));
        eventTypes.GetFor("event", 2U).Returns(new EventTypeSchema(new("event", 2), EventTypeOwner.Client, EventTypeSource.Code, second));
        eventTypes.Configure().GetDefinition("event").Returns(new EventTypeDefinition("event", EventTypeOwner.Client, false, [new(1, first), new(2, second)], [new(1, 2, [], new JsonObject(), new JsonObject())]));
        _command = _command with { Content = content, EventType = new("event", (uint)attemptedGeneration, false) };
        var plaintext = JsonNode.Parse(content)!.AsObject();
        var sourceSchema = attemptedGeneration == 1 ? first : second;
        var protectedSource = await _manager.Apply("store", "tenant", sourceSchema, "source", plaintext);
        var migrations = new ProtectedEventTypeMigrations(eventTypes, new EventTypeMigrations(_storage, _converter), _manager, _converter);
        var generations = await migrations.MigratePlaintext("store", "tenant", new("event", (uint)attemptedGeneration), plaintext, _converter.ToExpandoObject(protectedSource, sourceSchema), "source");
        var sequence = new EventSequenceStorage("store", "tenant", "log", new IdentityStorage());
        var appended = await sequence.Append(EventSequenceNumber.First, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, new("event", (uint)attemptedGeneration), CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, generations, new Dictionary<EventTypeGeneration, EventHash>());
        appended.IsSuccess.ShouldBeTrue();
        using var cursor = await sequence.GetRange(EventSequenceNumber.First, EventSequenceNumber.First);
        (await cursor.MoveNext()).ShouldBeTrue();
        _stored = cursor.Current.Single();
    }
}
