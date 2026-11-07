// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics.for_RepeatedFailureLog;

public class when_entries_are_not_failures : Specification
{
    RepeatedFailureLog _log;
    int _written;

    void Establish() => _log = new(TimeSpan.FromSeconds(10), 1, TimeProvider.System);

    void Because()
    {
        for (var i = 0; i < 10; i++)
        {
            if (_log.ShouldWrite("Category", LogLevel.Information, new TimeoutException()))
            {
                _written++;
            }

            if (_log.ShouldWrite("Category", LogLevel.Error, null))
            {
                _written++;
            }
        }
    }

    [Fact] void should_write_them_all() => _written.ShouldEqual(20);
}
