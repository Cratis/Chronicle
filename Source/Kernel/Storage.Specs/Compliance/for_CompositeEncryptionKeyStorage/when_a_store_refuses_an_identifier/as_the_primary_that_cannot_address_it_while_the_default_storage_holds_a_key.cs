// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_a_store_refuses_an_identifier;

public class as_the_primary_that_cannot_address_it_while_the_default_storage_holds_a_key : given.two_key_stores
{
    EncryptionKey? _served;
    Exception _error;

    async Task Establish()
    {
        await Save(_secondary, KeyNamed("default"));
        var refusing = Substitute.For<IEncryptionKeyStorage>();
        refusing.TryGetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKeyRevision?>()).ThrowsAsync(new InvalidEncryptionKeyAddress("identifier"));
        _composite = new CompositeEncryptionKeyStorage(refusing, _secondary);
    }

    async Task Because() => _error = await Catch.Exception(async () => _served = await _composite.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier));

    [Fact] public void should_refuse_the_read() => _error.ShouldBeOfExactType<InvalidEncryptionKeyAddress>();
    [Fact] public void should_not_serve_the_default_storage_key() => _served.ShouldBeNull();
}
