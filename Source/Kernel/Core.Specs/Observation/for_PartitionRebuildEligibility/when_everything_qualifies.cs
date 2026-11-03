// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_PartitionRebuildEligibility;

public class when_everything_qualifies : given.an_eligible_candidate
{
    void Because() => _result = PartitionRebuildEligibility.IsEligible(_features, _candidate);

    [Fact] void should_be_eligible() => _result.ShouldBeTrue();
}
