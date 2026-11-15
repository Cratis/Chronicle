// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.for_RepeatedFailureLog.given;
using Cratis.Chronicle.Diagnostics.for_RepeatedFailureLoggerProvider.given;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics.for_RepeatedFailureLoggerProvider;

public class when_more_kinds_storm_than_are_tracked : Specification
{
    RecordingLoggerProvider _inner;
    RepeatedFailureLoggerProvider _provider;
    IReadOnlyList<(string Category, LogLevel Level, string Message, Exception? Exception)> _entries;

    void Establish()
    {
        _inner = new();
        _provider = new(_inner, new RepeatedFailureLog(TimeSpan.FromSeconds(10), 1, new ManualTime(), maxKinds: 1, untrackedAllowedPerWindow: 1));
    }

    void Because()
    {
        _provider.CreateLogger("Tracked").ReadingFailed(new TimeoutException(), 0);
        for (var i = 0; i < 10; i++)
        {
            _provider.CreateLogger($"Untracked {i}").ReadingFailed(new TimeoutException(), i);
        }

        _provider.Dispose();
        _entries = _inner.Entries;
    }

    [Fact] void should_write_the_tracked_failure_the_first_untracked_one_and_a_summary() => _entries.Count.ShouldEqual(3);
    [Fact] void should_write_the_first_untracked_failure() => _entries[1].Category.ShouldEqual("Untracked 0");
    [Fact] void should_report_the_exception_type_and_count_of_the_untracked_failures() => _entries[2].Message.ShouldContain("9 more TimeoutException failures were counted but not written");
    [Fact] void should_not_report_a_placeholder_category() => _entries[2].Message.ShouldNotContain(RepeatedFailureLog.OverflowCategory);
}
