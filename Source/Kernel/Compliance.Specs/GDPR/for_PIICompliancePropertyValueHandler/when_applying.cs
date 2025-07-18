// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler;

public class when_applying : given.a_property_handler
{
    JsonNode _input;
    JsonNode _result;
    string _encryptedString = "Hello";
    byte[] _encryptedBytes;

    void Establish()
    {
        _input = JsonValue.Create(42);
        _encryptedBytes = Encoding.UTF8.GetBytes(_encryptedString);
        _encryption.Encrypt(Arg.Any<byte[]>(), _key).Returns(_encryptedBytes);
    }

    async Task Because() => _result = await _handler.Apply(string.Empty, string.Empty, Identifier, _input);

    [Fact] void should_return_encrypted_string() => _result.ToString().ShouldEqual(Convert.ToBase64String(_encryptedBytes));
}
