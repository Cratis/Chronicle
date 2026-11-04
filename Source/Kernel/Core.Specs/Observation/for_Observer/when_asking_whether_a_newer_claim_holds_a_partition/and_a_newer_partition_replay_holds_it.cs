// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_asking_whether_a_newer_claim_holds_a_partition;

/// <summary>
/// A newer replay does not read the batch a refused catch-up would have, so it does not cover it.
/// </summary>
public class and_a_newer_partition_replay_holds_it : when_a_job_reports_back.given.an_observer_holding_a_newer_claim
{
    bool _result;

    Task Establish() => HoldPartition(ClaimHolderKind.PartitionReplay);

    void Because() => _result = _observer.IsHeldByNewerClaim(_partition, StaleEpoch);

    [Fact] void should_not_be_held() => _result.ShouldBeFalse();
}
