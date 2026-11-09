// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ReleaseKeys;

public class when_wall_time_jumps_forward : given.a_key_store
{
    readonly given.a_release_clock _clock = new();
    EncryptionKey _result;

    async Task Because()
    {
        using var scope = ReleaseKeys.BeginScope(_clock);
        await Release(_keys);
        _keys.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(new EncryptionKeyErasure(2u, [], false, 1));
        _clock.JumpWall(TimeSpan.FromMinutes(1));
        _result = await Release(_keys);
    }

    [Fact] void should_expire_the_lease_without_monotonic_time_advancing() => _result.ShouldBeNull();
    [Fact] void should_read_the_fence_again() => _keys.Received(2).GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier);
}
