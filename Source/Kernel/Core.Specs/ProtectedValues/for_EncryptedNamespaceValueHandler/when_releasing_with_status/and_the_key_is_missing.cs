// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedNamespaceValueHandler.when_releasing_with_status;

public class and_the_key_is_missing : given.a_property_handler
{
    JsonNode _value;
    Exception _error;

    void Establish()
    {
        _value = JsonValue.Create(Convert.ToBase64String(new byte[256]));
        _encryption.IsEncrypted(Arg.Any<byte[]>()).Returns(true);
        _keyStore.TryGetFor(EventStore, EventStoreNamespace, KeyIdentifier).Returns(Task.FromResult<EncryptionKey?>(null));
        _keyStore.GetErasureFor(EventStore, EventStoreNamespace, KeyIdentifier).Returns(new EncryptionKeyErasure(1, [], false));
    }

    async Task Because() => _error = (await Catch.Exception(() => _handler.ReleaseWithStatus(EventStore, EventStoreNamespace, "original-subject", _value)))!;

    [Fact] void should_fail_instead_of_reporting_erasure() => _error.ShouldBeOfExactType<MissingEncryptionKey>();
    [Fact] async Task should_not_consult_erasure_records() => await _keyStore.DidNotReceive().GetErasureFor(EventStore, EventStoreNamespace, KeyIdentifier);
}
