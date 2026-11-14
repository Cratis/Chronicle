// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ProtectedValues.for_UnrecoverableProtectedValueReporter;

public class when_the_same_value_fails_concurrently : given.a_reporter
{
    void Because() => Parallel.For(0, 100, _ => _reporter.Report(Identity, _failure));

    [Fact] void should_increment_once() => _count.ShouldEqual(1);
    [Fact] void should_log_once() => _logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).ShouldEqual(1);
    [Fact] void should_not_log_sensitive_exception_details() => _logger.ReceivedCalls().SelectMany(call => call.GetArguments()).Any(arg => arg?.ToString()?.Contains("sensitive inner details") == true).ShouldBeFalse();
}
