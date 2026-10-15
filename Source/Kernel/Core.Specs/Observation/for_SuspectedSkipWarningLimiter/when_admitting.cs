// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_SuspectedSkipWarningLimiter;

public class when_admitting : Specification
{
    long _now;
    SuspectedSkipWarningLimiter _limiter;
    bool[] _firstWindow;
    int _suppressedWhenFirstWindowEnded;
    bool _secondWindowAdmitted;
    int _suppressedWhenSecondWindowOpened;
    int _suppressedWhenSecondWarningAdmitted;

    void Establish() => _limiter = new(10, 60_000, () => _now);

    void Because()
    {
        _firstWindow = Enumerable.Range(0, 12).Select(_ => _limiter.TryAdmit(out _suppressedWhenFirstWindowEnded)).ToArray();
        _now = 60_000;
        _secondWindowAdmitted = _limiter.TryAdmit(out _suppressedWhenSecondWindowOpened);
        _limiter.TryAdmit(out _suppressedWhenSecondWarningAdmitted);
    }

    [Fact] void should_admit_the_first_ten() => _firstWindow.Take(10).All(_ => _).ShouldBeTrue();
    [Fact] void should_not_admit_beyond_the_limit() => _firstWindow.Skip(10).Any(_ => _).ShouldBeFalse();
    [Fact] void should_not_report_a_summary_inside_the_window() => _suppressedWhenFirstWindowEnded.ShouldEqual(0);
    [Fact] void should_admit_in_the_next_window() => _secondWindowAdmitted.ShouldBeTrue();
    [Fact] void should_report_how_many_were_suppressed_once_when_the_next_window_opens() => _suppressedWhenSecondWindowOpened.ShouldEqual(2);
    [Fact] void should_not_report_the_summary_again() => _suppressedWhenSecondWarningAdmitted.ShouldEqual(0);
    [Fact] void should_expose_the_window_in_seconds() => _limiter.WindowSeconds.ShouldEqual(60);
}
