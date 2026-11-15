// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Setup;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// Pattern capture is optional and reconciled later, so waiting for a busy silo must not hold startup on it.
/// </summary>
public class and_pattern_capture_times_out_against_a_busy_silo : given.a_startup_task
{
    Exception _error;

    void Establish()
    {
        _busySilos.FindBusySilo(Arg.Any<Exception>()).Returns(new BusySilo("10.0.0.1:11111:42", true));
        _patternCapture.Subscribe(_eventStore, _namespace)
            .Returns(_ => Task.FromException(new TimeoutException("Response did not arrive on time")));
    }

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_not_fail_the_host() => _error.ShouldBeNull();
    [Fact] async Task should_give_up_after_one_round_of_attempts() => await _patternCapture.Received(5).Subscribe(_eventStore, _namespace);
}
