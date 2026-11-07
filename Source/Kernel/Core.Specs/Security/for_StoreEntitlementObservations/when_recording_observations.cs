// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_StoreEntitlementObservations;

public class when_recording_observations : Specification
{
    StoreEntitlementObservations _observations;
    StoreEntitlementObservation[] _result;

    void Establish() => _observations = new();

    async Task Because()
    {
        await _observations.Record(new("issuer", "first"), "orders", "tenant", StoreAccess.Read);
        await _observations.Record(new("issuer", "first"), "orders", "tenant", StoreAccess.Read);
        _result = (await _observations.GetSnapshot()).ToArray();
    }

    [Fact] void should_coalesce_the_same_scope() => _result.Length.ShouldEqual(1);
    [Fact] void should_count_observations() => _result[0].Count.ShouldEqual(2L);
    [Fact] void should_preserve_the_first_seen_time() => (_result[0].LastSeen >= _result[0].FirstSeen).ShouldBeTrue();
}
