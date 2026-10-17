// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering.for_Occurrences;

[Collection(OccurrenceCollection.Name)]
public class when_restarting_with_a_live_lease(OccurrenceFixture fixture)
{
    [Fact] public Task should_reclaim_at_the_next_epoch_and_complete_one_effect() => OccurrenceScenarios.ReclaimAfterTotalRestart(fixture);
}
