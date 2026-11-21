// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Releases;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.given;

public class a_signed_value_after_erasure : a_property_handler
{
    protected JsonNode _value;
    protected Exception _error;

    protected static EncryptionKeyErasure FenceRecording(params string[] fingerprints) => new(1, fingerprints, true);

    protected static JsonNode WithFlippedLastByte(JsonNode value)
    {
        var bytes = Convert.FromBase64String(value.ToString());
        bytes[^1] ^= 1;
        return JsonValue.Create(Convert.ToBase64String(bytes));
    }

    void Establish()
    {
        var releases = Substitute.For<IClusterReleases>();
        releases.Summarize().Returns(new ClusterReleaseSummary([(ClusterReleaseBoundary.KeyIdentity, 1)], 0, 0));
        var encryption = new Encryption(releases, new());
        _key = encryption.GenerateKey();
        _handler = new(_provisioner, _keyStore, encryption);
        _value = ProtectedValueCodec.Encrypt(encryption, _key, JsonValue.Create("value"), 2, new(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier));
        _keyStore.TryGetAtRevisionFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, new EncryptionKeyRevision(2)).Returns(Task.FromResult<EncryptionKeyAtRevision?>(null));
    }
}
