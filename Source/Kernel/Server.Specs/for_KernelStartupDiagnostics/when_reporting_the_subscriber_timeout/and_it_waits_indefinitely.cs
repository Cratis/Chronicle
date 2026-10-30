// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Server.for_KernelStartupDiagnostics.when_reporting_the_subscriber_timeout;

public class and_it_waits_indefinitely : Specification
{
    ILogger<Kernel> _logger;

    void Establish()
    {
        _logger = Substitute.For<ILogger<Kernel>>();
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
    }

    void Because() => KernelStartupDiagnostics.ReportSubscriberTimeout(new() { Observers = new() { SubscriberTimeout = 0 } }, _logger);

    [Fact] void should_warn_that_it_is_bounded() => _logger.ReceivedCalls()
        .Any(_ => _.GetMethodInfo().Name == "Log" && _.GetArguments()[0] is LogLevel.Warning &&
                  _.GetArguments()[1] is EventId eventId && eventId.Name == "SubscriberTimeoutIndefiniteIsBounded")
        .ShouldBeTrue();
}
