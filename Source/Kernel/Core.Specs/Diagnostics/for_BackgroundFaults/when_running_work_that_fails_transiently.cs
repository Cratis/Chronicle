// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics.for_BackgroundFaults;

public class when_running_work_that_fails_transiently : Specification
{
    ILogger _logger;
    Exception _exception;

    void Establish()
    {
        _logger = Substitute.For<ILogger>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    async Task Because()
    {
        try
        {
            await BackgroundFaults.Run(() => throw new TimeoutException(), _logger, "Owner");
        }
        catch (Exception ex)
        {
            _exception = ex;
        }
    }

    [Fact] void should_not_fault_the_task() => _exception.ShouldBeNull();

    [Fact] void should_log_a_warning() => _logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log) && (LogLevel)call.GetArguments()[0]! == LogLevel.Warning).ShouldEqual(1);
}
