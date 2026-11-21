// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ShutdownStageTimings;

public class when_participating : given.a_stage_timings_logger
{
    [Fact] void should_bracket_every_lifecycle_stage() => _stages.ShouldContainOnly(ShutdownStageTimings.Stages.Values);
    [Fact] void should_bracket_the_grain_deactivation_stage() => _stages.ShouldContain(ServiceLifecycleStage.RuntimeGrainServices);
}
