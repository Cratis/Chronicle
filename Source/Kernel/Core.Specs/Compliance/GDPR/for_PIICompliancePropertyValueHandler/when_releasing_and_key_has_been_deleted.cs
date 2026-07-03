// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler;

public class when_releasing_and_key_has_been_deleted : given.a_property_handler
{
    JsonNode _input;
    JsonNode _result;

    void Establish()
    {
        _input = JsonValue.Create(Convert.ToBase64String(Encoding.UTF8.GetBytes("encrypted")));
        _keyStore.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(Task.FromResult<EncryptionKey?>(null));
    }

    async Task Because() => _result = await _handler.Release(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _input);

    [Fact] void should_return_empty() => _result.ToString().ShouldEqual(string.Empty);
    [Fact] void should_not_attempt_to_decrypt() => _encryption.DidNotReceive().Decrypt(Arg.Any<byte[]>(), Arg.Any<EncryptionKey>());
}
