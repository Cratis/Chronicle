// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ReleaseKeys;

public class when_erased_material_reappears_at_a_later_revision : given.a_key_store
{
    EncryptionKey _result;

    void Establish() => _keys.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier)
        .Returns(new EncryptionKeyErasure(1, [_pair.Key.Fingerprint], true));

    async Task Because() => _result = await Release(_keys);

    [Fact] void should_refuse_the_erased_material() => _result.ShouldBeNull();
}
