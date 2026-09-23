// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedNamespaceValueHandler;

/// <summary>
/// The defining behavior of the namespace scope: every value in the namespace shares one key, regardless of which
/// document - which per-document identifier - it came from.
/// </summary>
public class when_applying_values_for_two_different_documents : given.a_property_handler
{
    void Establish() => _encryption.Encrypt(Arg.Any<byte[]>(), _key).Returns(Encoding.UTF8.GetBytes("cipher"));

    async Task Because()
    {
        await _handler.Apply(EventStore, EventStoreNamespace, "subject-a", JsonValue.Create("a"));
        await _handler.Apply(EventStore, EventStoreNamespace, "subject-b", JsonValue.Create("b"));
    }

    [Fact] async Task should_have_looked_up_the_same_key_for_both_documents() =>
        await _keyStore.Received(2).TryGetFor(EventStore, EventStoreNamespace, KeyIdentifier);
}
