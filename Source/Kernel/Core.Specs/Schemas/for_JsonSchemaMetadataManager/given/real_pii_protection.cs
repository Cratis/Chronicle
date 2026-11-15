// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.given;

public class real_pii_protection : Specification
{
    protected JsonSchemaMetadataManager _manager;
    protected IEncryptionKeyStorage _keys;
    protected JsonSchema _schema;
    protected JsonObject _plaintext;
    protected JsonObject _protected;
    protected JsonObject _result;

    void Establish()
    {
        var encryption = new Encryption();
        var key = encryption.GenerateKey();
        _keys = Substitute.For<IEncryptionKeyStorage>();
        _keys.TryGetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).Returns(key);
        _keys.TryGetLatestFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).Returns(new EncryptionKeyAtRevision(key, EncryptionKeyRevision.Initial));
        var provisioner = Substitute.For<IManagedEncryptionKeyProvisioner>();
        provisioner.EnsureKeyWithRevisionFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).Returns(new EncryptionKeyAtRevision(key, EncryptionKeyRevision.Initial));
        var handler = new PIICompliancePropertyValueHandler(provisioner, _keys, encryption);
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance);
    }

    protected static void Mark(JsonSchema schema) => schema.ExtensionData = new Dictionary<string, object?>
    {
        [ComplianceJsonSchemaExtensions.ComplianceKey] = new ComplianceSchemaMetadata[] { new(ComplianceMetadataType.PII.Value, string.Empty) }
    };

    protected async Task Protect() => _protected = await _manager.Apply("store", "tenant", _schema, "subject", _plaintext);
}
