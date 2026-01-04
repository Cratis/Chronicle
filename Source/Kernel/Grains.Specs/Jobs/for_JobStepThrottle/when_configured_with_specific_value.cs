// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Jobs.for_JobStepThrottle;

public class when_configured_with_specific_value : Specification
{
    Configuration.Jobs jobsConfig;
    int effectiveMaxParallelSteps;

    void Establish()
    {
        jobsConfig = new Configuration.Jobs { MaxParallelSteps = 5 };
    }

    void Because() => effectiveMaxParallelSteps = jobsConfig.GetEffectiveMaxParallelSteps();

    [Fact] void should_use_configured_value() => effectiveMaxParallelSteps.ShouldEqual(5);
}
