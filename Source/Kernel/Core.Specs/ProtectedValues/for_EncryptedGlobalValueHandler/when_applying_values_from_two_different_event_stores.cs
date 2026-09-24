// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedGlobalValueHandler;

/// <summary>
/// The defining behavior of the global scope: every value across the whole installation shares one key,
/// regardless of which real event store or namespace the protected value's own event actually belongs to.
/// </summary>
public class when_applying_values_from_two_different_event_stores : given.a_property_handler
{
    void Establish() => _encryption.Encrypt(Arg.Any<byte[]>(), _key).Returns(Encoding.UTF8.GetBytes("cipher"));

    async Task Because()
    {
        await _handler.Apply((EventStoreName)"FirstEventStore", (EventStoreNamespaceName)"FirstNamespace", "subject-a", JsonValue.Create("a"));
        await _handler.Apply((EventStoreName)"SecondEventStore", (EventStoreNamespaceName)"SecondNamespace", "subject-b", JsonValue.Create("b"));
    }

    [Fact] async Task should_have_looked_up_the_same_key_for_both_event_stores() =>
        await _keyStore.Received(2).TryGetFor(EncryptedValueKeyIdentifiers.GlobalEventStore, EncryptedValueKeyIdentifiers.GlobalNamespace, KeyIdentifier);

    [Fact] void should_never_have_looked_up_a_key_under_either_real_event_store()
    {
        _keyStore.DidNotReceive().TryGetFor((EventStoreName)"FirstEventStore", Arg.Any<EventStoreNamespaceName>(), Arg.Any<Compliance.EncryptionKeyIdentifier>());
        _keyStore.DidNotReceive().TryGetFor((EventStoreName)"SecondEventStore", Arg.Any<EventStoreNamespaceName>(), Arg.Any<Compliance.EncryptionKeyIdentifier>());
    }
}
