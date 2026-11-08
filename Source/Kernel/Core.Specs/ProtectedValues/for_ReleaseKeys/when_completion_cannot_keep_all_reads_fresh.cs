// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ReleaseKeys;

public class when_completion_cannot_keep_all_reads_fresh : given.a_key_store
{
    readonly given.a_release_clock _clock = new();
    Exception _error;
    bool _handedOff;
    int _observations;

    async Task Because()
    {
        using var scope = ReleaseKeys.BeginScope(_clock);
        await Release(_keys);
        ReleaseKeys.ObserveForCompletion(_keys, (EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier), _ =>
        {
            _observations++;
            _clock.Advance(TimeSpan.FromSeconds(3));
        });
        _error = await Catch.Exception(() => ReleaseKeys.Complete(() => _handedOff = true));
    }

    [Fact] void should_refuse_completion_at_the_overall_deadline() => _error.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
    [Fact] void should_not_hand_off_protected_material() => _handedOff.ShouldBeFalse();
    [Fact] void should_not_reset_the_budget_after_each_refresh() => _observations.ShouldEqual(5);
}
