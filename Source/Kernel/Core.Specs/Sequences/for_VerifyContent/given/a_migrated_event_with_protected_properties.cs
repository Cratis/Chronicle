// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.given;

public class a_migrated_event_with_protected_properties : a_stored_event
{
    protected Encryption _encryption;
    protected EncryptionKey _key;
    protected JsonSchema _first;
    protected JsonSchema _second;

    void Establish()
    {
        var keys = Substitute.For<IEncryptionKeyStorage>();
        _encryption = new Encryption();
        _key = _encryption.GenerateKey();
        keys.TryGetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).Returns(_key);
        var handler = new PIICompliancePropertyValueHandler(Substitute.For<IManagedEncryptionKeyProvisioner>(), keys, _encryption);
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance);

        _first = Schema(("name", true), ("kind", false));
        _second = Schema(("name", true), ("kind", false), ("alias", true), ("note", false));

        var eventTypes = _storage.GetEventStore("store").EventTypes;
        eventTypes.HasFor("event", 2U).Returns(true);
        eventTypes.GetFor("event", 1U).Returns(new EventTypeSchema(new("event", 1), EventTypeOwner.Client, EventTypeSource.Code, _first));
        eventTypes.GetFor("event", 2U).Returns(new EventTypeSchema(new("event", 2), EventTypeOwner.Client, EventTypeSource.Code, _second));
        _command = _command with { Content = """{"name":"a","kind":"x"}""" };
    }

    protected void MigrateWith(string upcast) =>
        _storage.GetEventStore("store").EventTypes.Configure().GetDefinition("event").Returns(new EventTypeDefinition(
            "event",
            EventTypeOwner.Client,
            false,
            [new(1, _first), new(2, _second)],
            [new(1, 2, [], JsonNode.Parse(upcast)!.AsObject(), new JsonObject())]));

    protected void Store(JsonObject first, JsonObject second) =>
        _stored = _stored with { GenerationalContent = new Dictionary<int, string> { [1] = first.ToJsonString(), [2] = second.ToJsonString() } };

    protected JsonNode Protect(string value) => ProtectedValueCodec.Encrypt(_encryption, _key, JsonValue.Create(value));

    static JsonSchema Schema(params (string Name, bool Protected)[] properties)
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        foreach (var (name, @protected) in properties)
        {
            schema.Properties[name] = new JsonSchemaProperty(name, new JsonObject(), schema)
            {
                Type = JsonObjectType.String,
                ExtensionData = @protected
                    ? new Dictionary<string, object?>
                    {
                        [ComplianceJsonSchemaExtensions.ComplianceKey] = new ComplianceSchemaMetadata[] { new(ComplianceMetadataType.PII.Value, string.Empty) }
                    }
                    : null
            };
        }

        return schema;
    }
}
