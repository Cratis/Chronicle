// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_PartitionRebuildAdmission;

public class when_generation_state_is_absent : given.an_admission_check
{
    async Task Because() => _result = await _admission.IsAdmitted(_request);

    [Fact] void should_admit_the_closed_projection() => _result.ShouldBeTrue();
}
