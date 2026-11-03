// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_PartitionRebuildAdmission;

public class when_the_floor_exists_without_a_binding : given.an_admission_check
{
    void Establish() => _store.GenerationFloor.Get().Returns(_floor);

    async Task Because() => _result = await _admission.IsAdmitted(_request);

    [Fact] void should_admit_the_closed_projection() => _result.ShouldBeTrue();
    [Fact] void should_have_checked_the_binding() => _bindings.Received(1).Get(_key.EventStore, _materialization);
}
