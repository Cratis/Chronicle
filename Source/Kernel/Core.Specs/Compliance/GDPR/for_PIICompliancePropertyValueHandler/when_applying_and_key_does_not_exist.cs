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
    EncryptionKey _provisionedKey;
    byte[] _encryptedBytes;

    void Establish()
    {
        _keyStore.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(Task.FromResult<EncryptionKey?>(null));
        _generatedKey = new EncryptionKey(Encoding.UTF8.GetBytes("NewPublic"), Encoding.UTF8.GetBytes("NewPrivate"));
        _encryption.GenerateKey().Returns(_generatedKey);

        // Provisioning goes through the atomic get-or-create so concurrent callers converge on one key pair.
        // The store echoes back the key that was actually persisted — here the freshly generated one.
        _provisionedKey = _generatedKey;
        _keyStore.GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, Arg.Any<EncryptionKey>()).Returns(Task.FromResult(_provisionedKey));

        _encryptedBytes = Encoding.UTF8.GetBytes("encrypted");
        _encryption.Encrypt(Arg.Any<byte[]>(), _provisionedKey).Returns(_encryptedBytes);
        _input = JsonValue.Create("sensitive");
    }

    async Task Because() => _result = await _handler.Apply(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _input);

    [Fact] void should_generate_a_new_key() => _encryption.Received(1).GenerateKey();
    [Fact] async Task should_provision_the_key_through_get_or_add() => await _keyStore.Received(1).GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _generatedKey);
    [Fact] async Task should_not_mint_a_separate_revision_with_save() => await _keyStore.DidNotReceive().SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, Arg.Any<EncryptionKey>());
    [Fact] void should_return_value_encrypted_with_the_provisioned_key() => _result.ToString().ShouldEqual(Convert.ToBase64String(_encryptedBytes));
}
