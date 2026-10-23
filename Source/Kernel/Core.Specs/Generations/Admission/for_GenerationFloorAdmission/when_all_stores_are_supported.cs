// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Admission.for_GenerationFloorAdmission;

public class when_all_stores_are_supported : given.an_admission
{
    Task Because() => _admission.Admit(CancellationToken.None);

    [Fact] void should_admit_the_first_listed_store() => _first.GenerationFloor.Received(1).Get();
    [Fact] void should_admit_the_second_listed_store() => _second.GenerationFloor.Received(1).Get();
    [Fact] void should_admit_system_even_when_it_is_not_listed() => _system.GenerationFloor.Received(1).Get();
    [Fact] void should_not_write_the_first_floor() => _first.GenerationFloor.DidNotReceiveWithAnyArgs().Advance(Arg.Any<GenerationFloorAdvance>());
    [Fact] void should_not_write_the_second_floor() => _second.GenerationFloor.DidNotReceiveWithAnyArgs().Advance(Arg.Any<GenerationFloorAdvance>());
    [Fact] void should_not_write_the_system_floor() => _system.GenerationFloor.DidNotReceiveWithAnyArgs().Advance(Arg.Any<GenerationFloorAdvance>());
}
