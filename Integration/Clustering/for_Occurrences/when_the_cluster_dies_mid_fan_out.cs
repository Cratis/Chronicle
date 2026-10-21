// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering.for_Occurrences;

[Collection(OccurrenceCollection.Name)]
public class when_the_cluster_dies_mid_fan_out(OccurrenceFixture fixture)
{
    [Fact] public Task should_recover_the_fixed_snapshot_without_duplicates() => OccurrenceScenarios.FanOutAfterMidwayCrash(fixture);
}
