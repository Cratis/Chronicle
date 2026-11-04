// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_asking_whether_a_newer_claim_holds_a_partition;

public class and_the_catch_up_claim_is_the_one_asked_about : when_a_job_reports_back.given.an_observer_holding_a_newer_claim
{
    bool _result;

    Task Establish() => HoldPartition(ClaimHolderKind.PartitionCatchUp);

    void Because() => _result = _observer.IsHeldByNewerClaim(_partition, CurrentEpoch);

    [Fact] void should_not_be_held() => _result.ShouldBeFalse();
}
