// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ReleaseKeys;

public class when_two_stores_share_an_identifier : given.a_key_store
{
    IEncryptionKeyStorage _other;
    EncryptionKey _first;
    EncryptionKey _second;

    void Establish()
    {
        _other = Substitute.For<IEncryptionKeyStorage>();
        _other.TryGetLatestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(_pair);
        _other.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(new EncryptionKeyErasure(2, [], false));
    }

    async Task Because()
    {
        using var scope = ReleaseKeys.BeginScope();
        _first = await Release(_keys);
        _second = await Release(_other);
    }

    [Fact] void should_release_from_the_unfenced_store() => _first.ShouldEqual(_pair.Key);
    [Fact] void should_not_reuse_another_stores_fence() => _second.ShouldBeNull();
    [Fact] void should_read_the_other_store() => _other.Received(1).GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier);
}
