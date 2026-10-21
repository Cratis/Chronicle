// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Clustering.for_Occurrences;

namespace Cratis.Chronicle.Kernel.Integration.Occurrences;

[Collection(SqlOccurrenceCollection.Name)]
public class when_the_kernel_dies_mid_fan_out(SqlOccurrenceFixture fixture)
{
    [Fact] public Task should_recover_the_fixed_snapshot_without_duplicates() => OccurrenceScenarios.FanOutAfterMidwayCrash(fixture);
}
