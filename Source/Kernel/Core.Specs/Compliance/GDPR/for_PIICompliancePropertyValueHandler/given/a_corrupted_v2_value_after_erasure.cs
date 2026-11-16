// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.given;

public class a_corrupted_v2_value_after_erasure : a_property_handler
{
    protected JsonNode _value;
    protected Exception _error;

    void Establish()
    {
        var encryption = new Encryption();
        _key = encryption.GenerateKey();
        _handler = new(_provisioner, _keyStore, encryption);
        _keyStore.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(new EncryptionKeyErasure(1, [], true));
        _keyStore.TryGetAtRevisionFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, new EncryptionKeyRevision(2)).Returns(new EncryptionKeyAtRevision(_key, 2));
        var encrypted = Encryption.EncryptEnvelope("\"value\""u8.ToArray(), new EncryptionKeyAtRevision(_key, 2), 2);
        encrypted[^1] ^= 1;
        _value = JsonValue.Create(Convert.ToBase64String(encrypted));
    }
}
