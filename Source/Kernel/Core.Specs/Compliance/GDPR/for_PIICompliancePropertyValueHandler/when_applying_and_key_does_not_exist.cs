// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler;

public class when_applying_and_key_does_not_exist : given.a_property_handler
{
    JsonNode _input;
    JsonNode _result;
    EncryptionKey _generatedKey;
    byte[] _encryptedBytes;

    void Establish()
    {
        _keyStore.HasFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(Task.FromResult(false));
        _generatedKey = new EncryptionKey(Encoding.UTF8.GetBytes("NewPublic"), Encoding.UTF8.GetBytes("NewPrivate"));
        _encryption.GenerateKey().Returns(_generatedKey);
        _encryptedBytes = Encoding.UTF8.GetBytes("encrypted");
        _encryption.Encrypt(Arg.Any<byte[]>(), _generatedKey).Returns(_encryptedBytes);
        _input = JsonValue.Create("sensitive");
    }

    async Task Because() => _result = await _handler.Apply(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _input);

    [Fact] void should_generate_a_new_key() => _encryption.Received(1).GenerateKey();
    [Fact] async Task should_save_the_generated_key_for_the_identifier() => await _keyStore.Received(1).SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _generatedKey);
    [Fact] void should_return_encrypted_value() => _result.ToString().ShouldEqual(Convert.ToBase64String(_encryptedBytes));
}
