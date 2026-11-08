// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection;

public class when_capturing_structured_diagnostics : Specification
{
    given.a_logger_that_remembers_messages<ChronicleConnection> _logger;
    Exception _failure;

    void Establish()
    {
        _logger = new();
        _failure = new InvalidServerAddress("synthetic-captured-address-marker");
    }

    void Because() => _logger.Log(
        LogLevel.Error,
        new EventId(1),
        new Dictionary<string, object?> { ["failure"] = _failure },
        _failure,
        (_, _) => "safe outer message");

    [Fact] void should_remember_the_formatted_message() => _logger.Messages.Single().ShouldEqual("safe outer message");
    [Fact] void should_preserve_the_actual_structured_exception() => ReferenceEquals(_logger.States.Single().Single().Value, _failure).ShouldBeTrue();
    [Fact] void should_preserve_the_actual_exception_parameter() => ReferenceEquals(_logger.Exceptions.Single(), _failure).ShouldBeTrue();
}
