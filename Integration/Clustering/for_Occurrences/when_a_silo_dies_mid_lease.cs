// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering.for_Occurrences;

[Collection(OccurrenceCollection.Name)]
public class when_a_silo_dies_mid_lease(OccurrenceFixture fixture)
{
    [Fact] public Task should_complete_one_effect_on_the_surviving_instance() => OccurrenceScenarios.LosingAnInstanceMidLease(fixture);
}
