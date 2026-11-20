// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ShutdownStageTimings;

public class when_stopping_the_stages : given.a_stage_timings_logger
{
    Task Because() => StopStagesInDescendingOrder(TimeSpan.FromSeconds(1));

    [Fact] void should_report_a_start_for_every_stage() => _logged.Count(_ => _.Message.Contains("began at", StringComparison.Ordinal)).ShouldEqual(_stages.Count);
    [Fact] void should_report_a_duration_for_every_stage_but_the_last() => _logged.Count(_ => _.Message.Contains("stopped in", StringComparison.Ordinal)).ShouldEqual(_stages.Count - 1);
    [Fact] void should_report_the_duration_of_a_stage() => _logged.ShouldContain(_ => _.Message.Contains("stopped in 00:00:01", StringComparison.Ordinal));
}
