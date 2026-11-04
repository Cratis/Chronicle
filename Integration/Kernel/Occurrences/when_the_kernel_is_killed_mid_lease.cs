// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Clustering.for_Occurrences;

namespace Cratis.Chronicle.Kernel.Integration.Occurrences;

[Collection(SqlOccurrenceCollection.Name)]
public class when_the_kernel_is_killed_mid_lease(SqlOccurrenceFixture fixture)
{
    [Fact] public Task should_reclaim_at_the_next_epoch_and_refuse_the_dead_holder() => OccurrenceScenarios.KillingTheKernelMidLease(fixture);
}
