// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_RunOptionsParser;

/// <summary>
/// Specifies defaults and invalid option rejection.
/// </summary>
public class when_parsing_options : Specification
{
    RunOptions _options = RunOptionsParser.Parse(["--seed", "1"]);

    void Because() => _options = RunOptionsParser.Parse([]);

    [Fact] void should_expose_a_generated_seed() => Assert.InRange(_options.Seed, 0, int.MaxValue - 1);
    [Fact] void should_default_to_two_minutes() => Assert.Equal(TimeSpan.FromMinutes(2), _options.Duration);
    [Fact] void should_default_to_sixty_seconds_settle() => Assert.Equal(TimeSpan.FromSeconds(60), _options.Settle);
    [Fact] void should_enable_all_faults() => Assert.Equal(Enum.GetValues<FaultKind>(), _options.Faults);
    [Fact] void should_keep_containers_opt_in() => Assert.False(_options.KeepContainers);
    [Fact] void should_disable_faults_explicitly() => Assert.Empty(RunOptionsParser.Parse(["--faults", "none"]).Faults);
    [Fact] void should_preserve_a_supplied_seed() => Assert.Equal(7, RunOptionsParser.Parse(["--seed", "7"]).Seed);
    [Fact] void should_reject_zero_duration() => Assert.Throws<ArgumentException>(() => RunOptionsParser.Parse(["--duration", "0s"]));
    [Fact] void should_reject_negative_duration() => Assert.Throws<ArgumentException>(() => RunOptionsParser.Parse(["--duration", "-1s"]));
    [Fact] void should_reject_unknown_duration_units() => Assert.Throws<ArgumentException>(() => RunOptionsParser.Parse(["--duration", "2z"]));
    [Fact] void should_reject_missing_duration() => Assert.Throws<ArgumentException>(() => RunOptionsParser.Parse(["--duration"]));
    [Fact] void should_reject_unknown_flags() => Assert.Throws<ArgumentException>(() => RunOptionsParser.Parse(["--unknown"]));
}
