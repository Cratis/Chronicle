// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ReleaseKeys;

public class when_a_new_operation_begins : given.a_key_store
{
    EncryptionKey _first;
    EncryptionKey _second;

    async Task Because()
    {
        using (ReleaseKeys.BeginScope())
        {
            _first = await Release(_keys);
        }
        _keys.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(new EncryptionKeyErasure(2, [], false));
        using (ReleaseKeys.BeginScope())
        {
            _second = await Release(_keys);
        }
    }

    [Fact] void should_release_before_erasure() => _first.ShouldEqual(_pair.Key);
    [Fact] void should_observe_erasure_in_the_next_operation() => _second.ShouldBeNull();
    [Fact] void should_read_the_fence_again() => _keys.Received(2).GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier);
}
