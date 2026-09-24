// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedSubjectValueHandler;

/// <summary>
/// Unlike PII, nothing ever deletes an [Encrypted] key through this codebase - see
/// <see cref="EncryptionKeyIsNotErasable"/>. This spec covers the shape anyway, because the key store contract
/// still allows a missing key (a restored backup pointed at a different key store, an operator's manual
/// intervention), and the handler must degrade the same way PII does rather than throw and fail an entire query.
/// </summary>
public class when_releasing_and_key_has_been_deleted : given.a_property_handler
{
    JsonNode _input;
    JsonNode _result;

    void Establish()
    {
        var stored = Encoding.UTF8.GetBytes("encrypted");
        _input = JsonValue.Create(Convert.ToBase64String(stored));
        _encryption.IsEncrypted(Arg.Any<byte[]>()).Returns(true);
        _keyStore.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, KeyIdentifier).Returns(Task.FromResult<EncryptionKey?>(null));
    }

    async Task Because() => _result = await _handler.Release(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _input);

    [Fact] void should_return_empty() => _result.ToString().ShouldEqual(string.Empty);
    [Fact] void should_not_attempt_to_decrypt() => _encryption.DidNotReceive().Decrypt(Arg.Any<byte[]>(), Arg.Any<EncryptionKey>());
}
