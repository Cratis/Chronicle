// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedGlobalValueHandler;

public class when_applying_and_key_does_not_exist : given.a_property_handler
{
    static readonly EventStoreName _eventStore = "SomeEventStore";
    static readonly EventStoreNamespaceName _eventStoreNamespace = "SomeNamespace";

    JsonNode _input;
    JsonNode _result;
    EncryptionKey _generatedKey;
    byte[] _encryptedBytes;

    void Establish()
    {
        _keyStore.TryGetFor(EncryptedValueKeyIdentifiers.GlobalEventStore, EncryptedValueKeyIdentifiers.GlobalNamespace, KeyIdentifier)
            .Returns(Task.FromResult<EncryptionKey?>(null));
        _generatedKey = new EncryptionKey(Encoding.UTF8.GetBytes("NewPublic"), Encoding.UTF8.GetBytes("NewPrivate"));
        _encryption.GenerateKey().Returns(_generatedKey);
        _keyStore.GetOrAddFor(EncryptedValueKeyIdentifiers.GlobalEventStore, EncryptedValueKeyIdentifiers.GlobalNamespace, KeyIdentifier, Arg.Any<EncryptionKey>())
            .Returns(Task.FromResult(_generatedKey));

        _encryptedBytes = Encoding.UTF8.GetBytes("encrypted");
        _encryption.Encrypt(Arg.Any<byte[]>(), _generatedKey).Returns(_encryptedBytes);
        _input = JsonValue.Create("sensitive");
    }

    /// <summary>
    /// Called with a real event store/namespace, exactly as the compliance walk would - proving the handler
    /// discards it in favor of the fixed global pair rather than merely happening to work when it is not given one.
    /// </summary>
    async Task Because() => _result = await _handler.Apply(_eventStore, _eventStoreNamespace, "some-subject", _input);

    [Fact] void should_generate_a_new_key() => _encryption.Received(1).GenerateKey();
    [Fact] async Task should_provision_the_key_under_the_fixed_global_pair() =>
        await _keyStore.Received(1).GetOrAddFor(EncryptedValueKeyIdentifiers.GlobalEventStore, EncryptedValueKeyIdentifiers.GlobalNamespace, KeyIdentifier, _generatedKey);
    [Fact] void should_return_value_encrypted_with_the_provisioned_key() => _result.ToString().ShouldEqual(Convert.ToBase64String(_encryptedBytes));
}
