// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Grains.Jobs.for_JobStepThrottle;

public class when_not_configured : Specification
{
    JobStepThrottle throttle;
    ChronicleOptions options;
    int effectiveMaxParallelSteps;

    void Establish()
    {
        options = new ChronicleOptions
        {
            Jobs = new Configuration.Jobs()
        };
        throttle = new JobStepThrottle(Options.Create(options), NullLogger<JobStepThrottle>.Instance);
        effectiveMaxParallelSteps = options.Jobs.GetEffectiveMaxParallelSteps();
    }

    [Fact] void should_use_default_value() => effectiveMaxParallelSteps.ShouldEqual(Math.Max(1, Environment.ProcessorCount - 1));
    
    [Fact] void should_never_be_less_than_one() => effectiveMaxParallelSteps.ShouldBeGreaterThanOrEqual(1);

    void Cleanup() => throttle.Dispose();
}
