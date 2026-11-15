// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_StoreEntitlementObservations;

public class when_exceeding_the_observation_limit : Specification
{
    StoreEntitlementObservations _observations;
    StoreEntitlementObservation[] _result;

    void Establish() => _observations = new();

    async Task Because()
    {
        for (var index = 0; index <= 5_000; index++)
        {
            await _observations.Record(new("issuer", index.ToString()), "orders", null, StoreAccess.Read);
        }
        _result = (await _observations.GetSnapshot()).ToArray();
    }

    [Fact] void should_bound_retained_identities() => _result.Length.ShouldEqual(5_000);
    [Fact] void should_evict_the_oldest_last_seen() => _result.Any(entry => entry.Holder.Subject == "0").ShouldBeFalse();
    [Fact] void should_retain_the_latest() => _result.Any(entry => entry.Holder.Subject == "5000").ShouldBeTrue();
}
