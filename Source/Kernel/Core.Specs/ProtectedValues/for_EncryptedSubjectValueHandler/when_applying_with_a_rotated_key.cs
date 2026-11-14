// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedSubjectValueHandler;

public class when_applying_with_a_rotated_key : given.a_property_handler
{
    void Establish()
    {
        _store.TryGetLatestFor(EventStoreName.NotSet, EventStoreNamespaceName.Default, "subject").Returns(new EncryptionKeyAtRevision(_key, 8));
        _encryption.Encrypt(Arg.Any<byte[]>(), _key, (EncryptionKeyRevision)8u).Returns([1]);
    }

    async Task Because() => await _handler.Apply(EventStoreName.NotSet, EventStoreNamespaceName.Default, "subject", JsonValue.Create("value"));

    [Fact] void should_pass_the_concrete_revision_to_encryption() => _encryption.Received(1).Encrypt(Arg.Any<byte[]>(), _key, (EncryptionKeyRevision)8u);
}
