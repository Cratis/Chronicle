// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_releasing_strictly;

public class and_key_storage_fails_with_recorded_erasure : given.a_property_handler
{
    Exception _error;
    JsonNode _value;

    void Establish()
    {
        _value = JsonValue.Create(Convert.ToBase64String(new byte[256]));
        _encryption.IsEncrypted(Arg.Any<byte[]>()).Returns(true);
        _keyStore.GetErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(new EncryptionKeyErasure(1, [], true));

        // Even a cryptographic failure originating in key storage is not a ciphertext unwrap failure.
        _keyStore.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).ThrowsAsync(new CryptographicException());
    }

    async Task Because() => _error = await Catch.Exception(() => _handler.ReleaseStrict(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _value));

    [Fact] void should_propagate_the_storage_failure() => _error.ShouldBeOfExactType<CryptographicException>();
    [Fact] void should_not_attempt_to_decrypt() => _encryption.DidNotReceive().Decrypt(Arg.Any<byte[]>(), _key);
}
