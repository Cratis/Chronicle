// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_releasing_strictly;

public class and_real_key_unwrapping_fails_without_erasure : Specification
{
    PIICompliancePropertyValueHandler _handler;
    JsonNode _value;
    Exception? _error;
    InMemoryEncryptionKeyStorage _keys;

    async Task Establish()
    {
        var encryption = new Encryption();
        _keys = new InMemoryEncryptionKeyStorage();
        _handler = new(new ManagedEncryptionKeyProvisioner(_keys, encryption), _keys, encryption);
        _value = ProtectedValueCodec.Encrypt(encryption, encryption.GenerateKey(), JsonValue.Create("protected value"));
        await _keys.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "owner", encryption.GenerateKey());
    }

    async Task Because() => _error = await Catch.Exception(() => _handler.ReleaseStrict(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "owner", _value));

    [Fact] void should_rethrow_the_classified_unwrap_failure() => _error.ShouldBeOfExactType<EncryptionKeyUnwrapFailed>();
    [Fact] void should_preserve_the_cryptographic_cause() => (_error!.InnerException is CryptographicException).ShouldBeTrue();
    [Fact] async Task should_have_no_recorded_erasure() => (await _keys.GetErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "owner")).ShouldBeNull();
}
