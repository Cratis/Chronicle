// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.for_RepeatedFailureLog.given;
using Cratis.Chronicle.Diagnostics.for_RepeatedFailureLoggerProvider.given;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics.for_RepeatedFailureLoggerProvider;

public class when_a_failure_storms : Specification
{
    ManualTime _time;
    RecordingLoggerProvider _inner;
    RepeatedFailureLoggerProvider _provider;
    IReadOnlyList<(string Category, LogLevel Level, string Message, Exception? Exception)> _entries;

    void Establish()
    {
        _time = new();
        _inner = new();
        _provider = new(_inner, new RepeatedFailureLog(TimeSpan.FromSeconds(10), 1, _time));
    }

    void Because()
    {
        var logger = _provider.CreateLogger("Storage");
        for (var i = 0; i < 1000; i++)
        {
            logger.ReadingFailed(new TimeoutException("wait queue full"), i);
        }

        logger.WritingFailed(new TimeoutException("wait queue full"), 1);
        logger.StillGoing();

        _time.Advance(TimeSpan.FromSeconds(11));
        _provider.Flush();
        _entries = _inner.Entries;
        _provider.Dispose();
    }

    [Fact] void should_write_four_entries() => _entries.Count.ShouldEqual(4);
    [Fact] void should_write_the_first_failure() => _entries[0].Message.ShouldEqual("Reading 0 failed");
    [Fact] void should_keep_the_level_of_the_first_failure() => _entries[0].Level.ShouldEqual(LogLevel.Error);
    [Fact] void should_keep_the_exception_of_the_first_failure() => _entries[0].Exception.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_write_another_failure_in_the_same_category() => _entries[1].Message.ShouldEqual("Writing 1 failed");
    [Fact] void should_write_entries_that_are_not_failures() => _entries[2].Message.ShouldEqual("Still going");
    [Fact] void should_summarize_the_repeats_without_an_exception() => _entries[3].Exception.ShouldBeNull();
    [Fact] void should_summarize_the_repeats_at_their_level() => _entries[3].Level.ShouldEqual(LogLevel.Error);
    [Fact] void should_count_the_repeats_in_the_summary() => _entries[3].Message.ShouldContain($"999 more TimeoutException failures from Storage ('{StorageLogging.ReadingFailedTemplate}')");
}
