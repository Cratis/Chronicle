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

namespace Cratis.Chronicle.Sequences.for_VerifyContent.given;

public class a_protected_event : a_stored_event
{
    protected IEncryptionKeyStorage _keys;
    protected Encryption _encryption;

    void Establish()
    {
        _keys = Substitute.For<IEncryptionKeyStorage>();
        _encryption = new Encryption();
        var key = _encryption.GenerateKey();
        _keys.TryGetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).Returns(key);
        var handler = new PIICompliancePropertyValueHandler(Substitute.For<IManagedEncryptionKeyProvisioner>(), _keys, _encryption);
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance);
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Properties["value"] = new JsonSchemaProperty("value", new JsonObject(), schema)
        {
            Type = JsonObjectType.String,
            ExtensionData = new Dictionary<string, object?>
            {
                [ComplianceJsonSchemaExtensions.ComplianceKey] = new ComplianceSchemaMetadata[] { new(ComplianceMetadataType.PII.Value, string.Empty) }
            }
        };
        _storage.GetEventStore("store").EventTypes.GetFor("event", 1U).Returns(new EventTypeSchema(new("event", 1), EventTypeOwner.Client, EventTypeSource.Code, schema));
        var content = new JsonObject { ["value"] = ProtectedValueCodec.Encrypt(_encryption, key, JsonValue.Create(string.Empty)) };
        _stored = _stored with { GenerationalContent = new Dictionary<int, string> { [1] = content.ToJsonString() } };
        _command = _command with { Content = "{\"value\":\"\"}" };
    }
}
