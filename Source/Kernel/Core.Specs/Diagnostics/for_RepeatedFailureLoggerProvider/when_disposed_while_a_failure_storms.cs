// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.for_RepeatedFailureLog.given;
using Cratis.Chronicle.Diagnostics.for_RepeatedFailureLoggerProvider.given;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics.for_RepeatedFailureLoggerProvider;

public class when_disposed_while_a_failure_storms : Specification
{
    RecordingLoggerProvider _inner;
    RepeatedFailureLoggerProvider _provider;
    IReadOnlyList<(string Category, LogLevel Level, string Message, Exception? Exception)> _entries;

    void Establish()
    {
        _inner = new();
        _provider = new(_inner, new RepeatedFailureLog(TimeSpan.FromSeconds(10), 1, new ManualTime()));
    }

    void Because()
    {
        var logger = _provider.CreateLogger("Storage");
        for (var i = 0; i < 10; i++)
        {
            logger.ReadingFailed(new TimeoutException("wait queue full"), i);
        }

        _provider.Dispose();
        _entries = _inner.Entries;
    }

    [Fact] void should_write_the_first_failure_and_a_summary() => _entries.Count.ShouldEqual(2);
    [Fact] void should_report_the_count_of_the_window_that_was_still_running() => _entries[1].Message.ShouldContain($"9 more TimeoutException failures from Storage ('{StorageLogging.ReadingFailedTemplate}')");
}
