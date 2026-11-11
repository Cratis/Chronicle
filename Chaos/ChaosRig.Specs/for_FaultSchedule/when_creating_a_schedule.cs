// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_FaultSchedule;

/// <summary>
/// Specifies deterministic, bounded and nonoverlapping fault plans.
/// </summary>
public class when_creating_a_schedule : Specification
{
    readonly RunOptions _options = RunOptionsParser.Parse(["--seed", "42"]);
    IReadOnlyList<ScheduledFault> _schedule = [];

    void Because() => _schedule = FaultSchedule.Create(_options);

    [Fact] void should_reproduce_the_seed() => Assert.Equal(_schedule, FaultSchedule.Create(_options));
    [Fact] void should_change_with_the_seed() => Assert.NotEqual(_schedule, FaultSchedule.Create(_options with { Seed = 43 }));
    [Fact] void should_be_nonempty() => Assert.NotEmpty(_schedule);
    [Fact] void should_stay_in_the_window() => Assert.All(_schedule, item => Assert.InRange(item.At, TimeSpan.Zero, _options.Duration));
    [Fact] void should_disable_faults() => Assert.Empty(FaultSchedule.Create(_options with { Faults = [] }));
    [Fact] void should_close_every_fault() => Assert.Equal(_schedule.Count(item => !item.Resume), _schedule.Count(item => item.Resume));

    [Fact]
    void should_not_overlap_even_on_different_targets()
    {
        for (var index = 0; index < _schedule.Count; index += 2)
        {
            Assert.False(_schedule[index].Resume);
            Assert.True(_schedule[index + 1].Resume);
            Assert.Equal(_schedule[index].Kind, _schedule[index + 1].Kind);
            Assert.True(_schedule[index].At < _schedule[index + 1].At);
            if (index + 2 < _schedule.Count) Assert.True(_schedule[index + 1].At < _schedule[index + 2].At);
        }
    }

    [Fact] void should_cross_the_configured_lease_ttl() => Assert.True(_schedule[1].At - _schedule[0].At > TimeSpan.FromSeconds(10));
}
