// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler;

public class when_trying_to_release_and_key_unwrap_fails : given.a_property_handler
{
    JsonNode _value;
    EncryptionKeyUnwrapFailed _failure;
    Exception _error;

    void Establish()
    {
        _value = JsonValue.Create(Convert.ToBase64String(new byte[256]));
        _failure = new(new CryptographicException("unwrap failed"));
        _encryption.IsEncrypted(Arg.Any<byte[]>()).Returns(true);
        _encryption.Decrypt(Arg.Any<byte[]>(), _key).Returns(_ => throw _failure);
    }

    async Task Because() => _error = await Catch.Exception(() => _handler.TryRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _value));

    [Fact] void should_preserve_the_original_unwrap_failure() => _error.ShouldBeSame(_failure);
}
