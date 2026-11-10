// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.for_RepeatedFailureLog.given;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics.for_RepeatedFailureLog;

public class when_more_kinds_fail_than_are_tracked : Specification
{
    const int MaxKinds = 4;
    const int UntrackedAllowedPerWindow = 2;
    const int Failures = 100;

    ManualTime _time;
    RepeatedFailureLog _log;
    int _writtenUntracked;
    IReadOnlyList<SuppressedFailures> _ended;
    IReadOnlyList<SuppressedFailures> _endedAfterForgetting;
    int _writtenAfterForgetting;

    void Establish()
    {
        _time = new();
        _log = new(TimeSpan.FromSeconds(10), 1, _time, MaxKinds, UntrackedAllowedPerWindow);
    }

    void Because()
    {
        for (var i = 0; i < MaxKinds; i++)
        {
            _log.ShouldWrite("Category", $"Failure {i}", LogLevel.Error, new TimeoutException());
        }

        for (var i = MaxKinds; i < Failures; i++)
        {
            var written = i % 2 == 0
                ? _log.ShouldWrite("Category", $"Failure {i}", LogLevel.Error, new TimeoutException())
                : _log.ShouldWrite("Category", $"Failure {i}", LogLevel.Warning, new InvalidOperationException());
            if (written)
            {
                _writtenUntracked++;
            }
        }

        _time.Advance(TimeSpan.FromSeconds(11));
        _ended = _log.TakeEnded();
        _endedAfterForgetting = _log.TakeEnded();

        for (var i = Failures; i < Failures + MaxKinds; i++)
        {
            if (_log.ShouldWrite("Category", $"Failure {i}", LogLevel.Error, new TimeoutException()))
            {
                _writtenAfterForgetting++;
            }
        }
    }

    [Fact] void should_write_the_first_failures_beyond_the_bound_up_to_their_shared_budget() => _writtenUntracked.ShouldEqual(UntrackedAllowedPerWindow);
    [Fact] void should_report_two_counts() => _ended.Count.ShouldEqual(2);

    [Fact]
    void should_count_the_rest_of_one_exception_type_at_its_level() =>
        _ended.ShouldContain(new SuppressedFailures(RepeatedFailureLog.OverflowCategory, string.Empty, typeof(TimeoutException), ((Failures - MaxKinds) / 2) - 1, LogLevel.Error));

    [Fact]
    void should_count_the_rest_of_the_other_exception_type_at_its_level() =>
        _ended.ShouldContain(new SuppressedFailures(RepeatedFailureLog.OverflowCategory, string.Empty, typeof(InvalidOperationException), ((Failures - MaxKinds) / 2) - 1, LogLevel.Warning));

    [Fact] void should_report_them_as_untracked() => _ended.All(_ => _.IsUntracked).ShouldBeTrue();
    [Fact] void should_have_nothing_more_to_report() => _endedAfterForgetting.ShouldBeEmpty();
    [Fact] void should_track_new_kinds_once_the_old_ones_are_forgotten() => _writtenAfterForgetting.ShouldEqual(MaxKinds);
}
