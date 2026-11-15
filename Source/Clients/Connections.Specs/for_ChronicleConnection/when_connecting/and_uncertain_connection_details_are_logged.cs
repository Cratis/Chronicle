// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_uncertain_connection_details_are_logged : given.a_connection_that_lost_a_working_kernel
{
    const string Username = "synthetic-logger-user-marker";
    const string NumericPassword = "1753198871";
    const string Tail = "synthetic-logger-tail-marker";
    Exception _failure;

    protected override ChronicleConnectionString ConnectionString => new($"chronicle://{Username}:{NumericPassword}?apiKey={Tail}@server:35000");

    async Task Because()
    {
        await ConnectThenLoseTheKernel();
        await Catch.Exception(_connection.Connect);
        _failure = await Catch.Exception(_connection.Connect);
    }

    [Fact] void should_still_report_the_backoff_failure() => _failure.ShouldBeOfExactType<ConnectionUnavailable>();
    [Fact] void should_capture_a_connection_attempt() => _logger.Messages.ShouldContain(message => message.Contains("Connecting to Chronicle", StringComparison.Ordinal));
    [Fact] void should_capture_the_validation_warning() => _logger.States.SelectMany(state => state).Any(argument => argument.Key == "Servers").ShouldBeTrue();
    [Fact] void should_capture_structured_arguments() => _logger.States.SelectMany(state => state).Any(argument => argument.Key != "{OriginalFormat}").ShouldBeTrue();
    [Fact] void should_not_expose_markers_in_formatted_logs() => AssertSafe(_logger.Messages);
    [Fact] void should_not_expose_markers_in_structured_values() => AssertSafe(_logger.States.SelectMany(state => state).SelectMany(argument => given.diagnostic_payloads.Text(argument.Value)));
    [Fact] void should_not_expose_markers_in_logged_exception_objects() => AssertSafe(_logger.Exceptions.SelectMany(given.diagnostic_payloads.Text));
    [Fact] void should_not_expose_markers_in_the_actual_backoff_exception() => AssertSafe(given.diagnostic_payloads.Text(_failure));

    static void AssertSafe(IEnumerable<string> values) => given.diagnostic_payloads.ShouldNotExpose(values, Username, NumericPassword, "175319", Tail);
}
