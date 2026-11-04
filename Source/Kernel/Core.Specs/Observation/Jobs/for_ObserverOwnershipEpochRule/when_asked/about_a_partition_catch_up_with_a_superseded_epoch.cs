// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverOwnershipEpochRule.when_asked;

public class about_a_partition_catch_up_with_a_superseded_epoch : given.a_rule
{
    void Establish() => PersistPartitionClaim(ClaimHolderKind.PartitionCatchUp, 8);
    async Task Because() => _result = await Ask(PartitionCatchUp(7));

    [Fact] void should_refuse_it() => _result.ShouldBeFalse();
}
