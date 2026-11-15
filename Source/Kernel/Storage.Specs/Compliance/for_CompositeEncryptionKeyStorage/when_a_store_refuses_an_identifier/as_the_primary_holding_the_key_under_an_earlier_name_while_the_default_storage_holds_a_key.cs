// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_a_store_refuses_an_identifier;

public class as_the_primary_holding_the_key_under_an_earlier_name_while_the_default_storage_holds_a_key : given.two_key_stores
{
    IEncryptionKeyStorage _refusing;
    EncryptionKey? _served;
    Exception _readError;
    Exception _provisionError;

    async Task Establish()
    {
        await Save(_secondary, KeyNamed("default"));
        _refusing = Substitute.For<IEncryptionKeyStorage>();
        _refusing.FenceGuarantees.Returns(EncryptionKeyFenceGuarantees.MonotonicFence | EncryptionKeyFenceGuarantees.FencedSave);
        _refusing.TryGetLatestFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).ThrowsAsync(new EncryptionKeyNeedsUpgrade());
        _refusing.GetErasureForRelease(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).ThrowsAsync(new EncryptionKeyNeedsUpgrade());
        _refusing.TryGetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKeyRevision?>()).ThrowsAsync(new EncryptionKeyNeedsUpgrade());
        _refusing.GetOrAddFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKey>()).ThrowsAsync(new EncryptionKeyNeedsUpgrade());
        _composite = new CompositeEncryptionKeyStorage(_refusing, _secondary);
    }

    async Task Because()
    {
        _readError = await Catch.Exception(async () => _served = await _composite.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier));
        _provisionError = await Catch.Exception(() => _composite.GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, KeyNamed("fresh")));
    }

    [Fact] void should_refuse_the_read() => _readError.ShouldBeOfExactType<EncryptionKeyNeedsUpgrade>();
    [Fact] void should_not_serve_the_default_storage_key() => _served.ShouldBeNull();
    [Fact] void should_refuse_provisioning() => _provisionError.ShouldBeOfExactType<EncryptionKeyNeedsUpgrade>();
    [Fact] async Task should_keep_the_default_storage_key_unchanged() => (await KeyIn(_secondary)).Fingerprint.ShouldEqual(KeyNamed("default").Fingerprint);
}
