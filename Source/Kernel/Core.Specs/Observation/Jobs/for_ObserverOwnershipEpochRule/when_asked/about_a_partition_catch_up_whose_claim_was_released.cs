// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverOwnershipEpochRule.when_asked;

public class about_a_partition_catch_up_whose_claim_was_released : given.a_rule
{
    async Task Because() => _result = await Ask(PartitionCatchUp(7));

    [Fact] void should_refuse_it() => _result.ShouldBeFalse();
}
