// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedNamespaceValueHandler;

public class when_releasing : given.a_property_handler
{
    JsonNode _input;
    JsonNode _result;
    string _decryptedString = "Hello";
    byte[] _decryptedBytes;

    void Establish()
    {
        _input = JsonValue.Create(Convert.ToBase64String(Encoding.UTF8.GetBytes("42")));
        _decryptedBytes = Encoding.UTF8.GetBytes(_decryptedString);
        _encryption.IsEncrypted(Arg.Any<byte[]>()).Returns(true);
        _encryption.Decrypt(Arg.Any<byte[]>(), _key).Returns(_decryptedBytes);
    }

    async Task Because() => _result = await _handler.Release(EventStore, EventStoreNamespace, "some-subject", _input);

    [Fact] void should_return_decrypted_string() => _result.ToString().ShouldEqual(_decryptedString);
}
