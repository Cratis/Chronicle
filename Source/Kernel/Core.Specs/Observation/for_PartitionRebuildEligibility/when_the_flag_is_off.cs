// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_PartitionRebuildEligibility;

public class when_the_flag_is_off : given.an_eligible_candidate
{
    void Establish() => _features = new() { IsolatedPartitionReplay = false };

    void Because() => _result = PartitionRebuildEligibility.IsEligible(_features, _candidate);

    [Fact] void should_not_be_eligible() => _result.ShouldBeFalse();
}
