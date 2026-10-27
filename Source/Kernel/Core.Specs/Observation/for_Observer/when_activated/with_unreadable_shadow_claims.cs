// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_activated;

public class with_unreadable_shadow_claims : given.an_observer_with_metrics
{
    async Task Establish()
    {
        var claims = Substitute.For<IEnumerable<PartitionClaim>>();
        claims.GetEnumerator().Returns(_ => throw new InvalidOperationException("Unreadable shadow diagnostics"));
        _stateStorage.State = _stateStorage.State with { PartitionClaims = claims, OwnershipRevision = new(7) };
        await Crash();
        _storageStats.ResetCounts();
    }

    async Task Because() => await _observer.RegisterCatchingUpPartitions([]);

    [Fact] void should_still_write_observer_state() => _storageStats.Writes.ShouldEqual(1);
    [Fact] void should_not_quarantine_the_observer() => _stateStorage.State.RunningState.HasFlag(ObserverRunningState.Quarantined).ShouldBeFalse();
    [Fact] void should_replace_unreadable_diagnostics_with_a_safe_snapshot() => _stateStorage.State.PartitionClaims.ShouldBeEmpty();
}
