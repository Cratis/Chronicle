// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_RunBudget;

public class when_the_budget_has_time_left : Specification
{
    RunBudget _budget = default!;
    bool _initiallyExpired;
    TimeSpan _remaining;
    void Because()
    {
        var clock = new Clock();
        _budget = new(TimeSpan.FromMinutes(2), clock);
        _initiallyExpired = _budget.Expired;
        clock.Elapsed = TimeSpan.FromMinutes(1);
        _remaining = _budget.Remaining;
        clock.Elapsed = TimeSpan.FromMinutes(2);
    }
    void Destroy() => _budget.Dispose();
    [Fact] void should_start_with_time_left() => _initiallyExpired.ShouldBeFalse();
    [Fact] void should_report_remaining_time() => _remaining.ShouldEqual(TimeSpan.FromMinutes(1));
    [Fact] void should_expire_after_advancing_the_clock() => _budget.Expired.ShouldBeTrue();
    [Fact] void should_cancel_after_advancing_the_clock() => _budget.Token.IsCancellationRequested.ShouldBeTrue();

    internal sealed class Clock : TimeProvider
    {
        public TimeSpan Elapsed { get; set; }
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Elapsed.Ticks;
    }
}
