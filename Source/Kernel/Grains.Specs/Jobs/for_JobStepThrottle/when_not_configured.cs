// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Grains.Jobs.for_JobStepThrottle;

public class when_not_configured : Specification
{
    JobStepThrottle _throttle;
    ChronicleOptions _options;
    int _effectiveMaxParallelSteps;

    void Establish()
    {
        _options = new ChronicleOptions
        {
            Jobs = new Configuration.Jobs()
        };
        _throttle = new JobStepThrottle(Options.Create(_options), NullLogger<JobStepThrottle>.Instance);
        _effectiveMaxParallelSteps = _options.Jobs.GetEffectiveMaxParallelSteps();
    }

    [Fact] void should_use_default_value() => _effectiveMaxParallelSteps.ShouldEqual(Math.Max(1, Environment.ProcessorCount - 1));

    [Fact] void should_never_be_less_than_one() => _effectiveMaxParallelSteps.ShouldBeGreaterThanOrEqual(1);

    void Cleanup() => _throttle.Dispose();
}
