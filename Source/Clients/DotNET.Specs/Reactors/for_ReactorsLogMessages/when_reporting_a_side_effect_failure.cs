// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ReactorsLogMessages;

public class when_reporting_a_side_effect_failure : Specification
{
    ILogger<Reactors> _logger;
    string _warning;

    void Establish()
    {
        _logger = Substitute.For<ILogger<Reactors>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    void Because()
    {
        _logger.ReactorSideEffectAppendFailed("event", "reactor", 2);
        _logger.ReactorSideEffectAppendFailureDetails("reactor", "Failed appending to customer-42");
        _warning = _logger.ReceivedCalls().Single(_ => _.GetMethodInfo().Name == "Log" && (LogLevel)_.GetArguments()[0]! == LogLevel.Warning).GetArguments()[2].ToString();
    }

    [Fact] void should_log_the_target_count() => _warning.Contains("2 event sources", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_log_raw_target_ids_at_warning() => _warning.Contains("customer-42", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_attach_target_ids_to_warning_state() => ((IEnumerable<KeyValuePair<string, object>>)_logger.ReceivedCalls().Single(_ => _.GetMethodInfo().Name == "Log" && (LogLevel)_.GetArguments()[0]! == LogLevel.Warning).GetArguments()[2]).Any(_ => _.Value?.ToString()?.Contains("customer-42", StringComparison.Ordinal) == true).ShouldBeFalse();
    [Fact] void should_keep_the_details_at_debug() => _logger.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == "Log" && (LogLevel)_.GetArguments()[0]! == LogLevel.Debug && _.GetArguments()[2].ToString().Contains("customer-42", StringComparison.Ordinal)).ShouldBeTrue();
}
