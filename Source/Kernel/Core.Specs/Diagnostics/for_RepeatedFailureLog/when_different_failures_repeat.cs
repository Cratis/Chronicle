// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics.for_RepeatedFailureLog;

public class when_different_failures_repeat : Specification
{
    RepeatedFailureLog _log;
    bool[] _written;

    void Establish() => _log = new(TimeSpan.FromSeconds(10), 1, TimeProvider.System);

    void Because()
    {
        _log.ShouldWrite("Category", "Reading failed", LogLevel.Error, new TimeoutException());
        _written =
        [
            _log.ShouldWrite("Category", "Writing failed", LogLevel.Error, new TimeoutException()),
            _log.ShouldWrite("Category", "Reading failed", LogLevel.Error, new InvalidOperationException()),
            _log.ShouldWrite("Other", "Reading failed", LogLevel.Error, new TimeoutException()),
        ];
    }

    [Fact] void should_write_another_message_in_the_same_category() => _written[0].ShouldBeTrue();
    [Fact] void should_write_another_root_exception_with_the_same_message() => _written[1].ShouldBeTrue();
    [Fact] void should_write_the_same_failure_from_another_category() => _written[2].ShouldBeTrue();
}
