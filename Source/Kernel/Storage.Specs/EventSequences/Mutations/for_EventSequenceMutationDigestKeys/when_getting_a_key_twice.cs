// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationDigestKeys;

public class when_getting_a_key_twice : given.a_key_store
{
    IEncryptionKeyStorage _counting;
    EventSequenceMutationDigestKey? _first;
    EventSequenceMutationDigestKey? _second;
    EventSequenceMutationDigestKey? _other;

    void Establish()
    {
        _counting = Substitute.For<IEncryptionKeyStorage>();
        _counting.GetOrAddFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKey>())
            .Returns(call => _keyStore.GetOrAddFor(call.ArgAt<EventStoreName>(0), call.ArgAt<EventStoreNamespaceName>(1), call.ArgAt<EncryptionKeyIdentifier>(2), call.ArgAt<EncryptionKey>(3)));
    }

    async Task Because()
    {
        var keys = KeysOver(_counting);
        _first = await keys.TryGetFor("event-store");
        _second = await keys.TryGetFor("event-store");
        _other = await keys.TryGetFor("other-store");
    }

    [Fact] void should_return_the_cached_key() => ReferenceEquals(_second, _first).ShouldBeTrue();
    [Fact] void should_read_the_key_store_once_per_event_store() =>
        _counting.Received(2).GetOrAddFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKey>());

    [Fact] void should_store_the_secret_under_the_reserved_identifier_in_the_default_namespace() =>
        _counting.Received(1).GetOrAddFor("event-store", EventStoreNamespaceName.Default, EventSequenceMutationDigestKeyIdentifiers.Secret, Arg.Any<EncryptionKey>());

    [Fact] void should_key_each_event_store_differently() => _first!.Compute(_canonical).SequenceEqual(_other!.Compute(_canonical)).ShouldBeFalse();
}
