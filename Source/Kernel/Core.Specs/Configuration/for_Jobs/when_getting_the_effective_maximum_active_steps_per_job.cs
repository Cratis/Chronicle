// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_Jobs;

public class when_getting_the_effective_maximum_active_steps_per_job : Specification
{
    int _default;
    int _scaledToParallelSteps;
    int _configured;
    int _belowOne;

    void Because()
    {
        _default = new Jobs { MaxParallelSteps = 2 }.GetEffectiveMaxActiveStepsPerJob();
        _scaledToParallelSteps = new Jobs { MaxParallelSteps = 20 }.GetEffectiveMaxActiveStepsPerJob();
        _configured = new Jobs { MaxActiveStepsPerJob = 5 }.GetEffectiveMaxActiveStepsPerJob();
        _belowOne = new Jobs { MaxActiveStepsPerJob = 0 }.GetEffectiveMaxActiveStepsPerJob();
    }

    [Fact] void should_never_default_below_32() => _default.ShouldEqual(32);
    [Fact] void should_default_to_four_times_the_parallel_steps() => _scaledToParallelSteps.ShouldEqual(80);
    [Fact] void should_use_the_configured_value() => _configured.ShouldEqual(5);
    [Fact] void should_treat_a_value_below_one_as_one() => _belowOne.ShouldEqual(1);
}
