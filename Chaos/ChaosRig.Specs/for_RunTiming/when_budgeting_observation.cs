// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_RunTiming;

/// <summary>
/// Specifies that recovery consumes drain time rather than canceling snapshot finalization.
/// </summary>
public class when_budgeting_observation : Specification
{
    RunOptions _options;
    RunOptions _observation;

    void Establish() => _options = new(1, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(1), [], "unused", false, false);

    void Because() => _observation = RunTiming.ForObservation(_options, TimeSpan.FromSeconds(140));

    [Fact] void should_allocate_only_the_unspent_drain_time() => _observation.Settle.ShouldEqual(TimeSpan.FromSeconds(40));
    [Fact] void should_preserve_the_original_settle_option() => _options.Settle.ShouldEqual(TimeSpan.FromMinutes(1));
    [Fact] void should_preserve_other_run_options() => (_observation with { Settle = _options.Settle }).ShouldEqual(_options);
    [Fact] void should_not_restart_an_expired_drain() => RunTiming.ForObservation(_options, TimeSpan.FromMinutes(4)).Settle.ShouldEqual(TimeSpan.Zero);
}
