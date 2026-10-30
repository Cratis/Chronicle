// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_a_key;

public class and_healing_the_primary_store_fails : given.two_key_stores
{
    IEncryptionKeyStorage _readableButUnwritablePrimary;
    EncryptionKey _key;
    EncryptionKey? _result;
    Exception _error;

    async Task Establish()
    {
        _key = KeyNamed("secondary");
        await Save(_secondary, _key);

        _readableButUnwritablePrimary = Substitute.For<IEncryptionKeyStorage>();
        _readableButUnwritablePrimary
            .TryGetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKeyRevision?>())
            .Returns((EncryptionKey?)null);
        _readableButUnwritablePrimary
            .SaveFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKey>(), Arg.Any<EncryptionKeyRevision?>())
            .ThrowsAsync(new StoreUnreachable());

        _composite = new CompositeEncryptionKeyStorage(_readableButUnwritablePrimary, _secondary);
    }

    async Task Because() => _error = await Catch.Exception(async () => _result = await _composite.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier));

    [Fact] void should_still_return_the_key() => _result.ShouldEqual(_key);
    [Fact] void should_not_turn_a_failed_heal_into_a_failed_read() => _error.ShouldBeNull();
    [Fact] void should_have_attempted_the_heal() => _readableButUnwritablePrimary.Received(1).SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _key, null);
}
