// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Generations;

namespace Cratis.Chronicle.Generations.Admission.for_GenerationFloorAdmission;

public class when_system_is_below_floor : given.an_admission
{
    Exception _error;

    void Establish() => _system.GenerationFloor.Get().Returns(Task.FromResult<GenerationFloor?>(Floor(2)));

    async Task Because() => _error = await Catch.Exception(() => _admission.Admit(CancellationToken.None));

    [Fact] void should_refuse_admission() => _error.ShouldBeOfExactType<KernelBelowGenerationFloor>();
    [Fact] void should_not_write_the_first_floor() => _first.GenerationFloor.DidNotReceiveWithAnyArgs().Advance(Arg.Any<GenerationFloorAdvance>());
    [Fact] void should_not_write_the_second_floor() => _second.GenerationFloor.DidNotReceiveWithAnyArgs().Advance(Arg.Any<GenerationFloorAdvance>());
    [Fact] void should_not_write_the_system_floor() => _system.GenerationFloor.DidNotReceiveWithAnyArgs().Advance(Arg.Any<GenerationFloorAdvance>());
}
