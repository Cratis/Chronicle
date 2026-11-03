// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Observation.for_PartitionRebuildAdmission;

public class when_a_floor_appears_during_inspection : given.an_admission_check
{
    void Establish() => _store.GenerationFloor.Get().Returns(Task.FromResult<GenerationFloor?>(null), Task.FromResult<GenerationFloor?>(_floor));

    async Task Because() => _result = await _admission.IsAdmitted(_request);

    [Fact] void should_fail_closed() => _result.ShouldBeFalse();
}
