// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ReleaseKeys;

public class when_every_refresh_exceeds_the_lease : given.a_key_store
{
    readonly given.a_release_clock _clock = new();
    Exception _error;
    int _reads;

    void Establish() => _keys.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(_ =>
    {
        _reads++;
        _clock.Advance(TimeSpan.FromSeconds(3));
        return Task.FromResult<EncryptionKeyErasure?>(null);
    });

    async Task Because()
    {
        using var scope = ReleaseKeys.BeginScope(_clock);
        _error = await Catch.Exception(() => Release(_keys));
    }

    [Fact] void should_refuse_release_when_the_overall_deadline_expires() => _error.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
    [Fact] void should_bound_the_number_of_refreshes() => _reads.ShouldEqual(5);
    [Fact] void should_stop_after_three_grace_periods() => _clock.GetUtcNow().ShouldEqual(DateTimeOffset.UnixEpoch + TimeSpan.FromSeconds(15));
}
