// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ReleaseKeys;

public class when_a_staged_key_belongs_to_an_erased_lifecycle : given.a_key_store
{
    EncryptionKey _result;

    void Establish()
    {
        _keys.TryGetLatestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier)
            .Returns(new EncryptionKeyAtRevision(_pair.Key, 999u, 1));
        _keys.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier)
            .Returns(new EncryptionKeyErasure(2u, [], true, 2) { ErasedLifecycle = 1 });
    }

    async Task Because() => _result = await Release(_keys);

    [Fact] void should_refuse_even_above_the_revision_floor_with_unrecorded_material() => _result.ShouldBeNull();
}
