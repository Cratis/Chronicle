// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers;

public class when_converting_a_string_that_fails_conversion : given.an_appended_event
{
    const string Value = "not-a-guid-acme";

    ILogger<EventValueProviderExpressionResolvers> _logger;
    EventValueProviderExpressionResolvers _resolvers;
    object _result;

    void Establish()
    {
        _logger = Substitute.For<ILogger<EventValueProviderExpressionResolvers>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _resolvers = new(new TypeFormats(), _logger);
        ((IDictionary<string, object?>)@event.Content)["Payload"] = Value;
    }

    void Because() => _result = _resolvers.Resolve(new JsonSchemaProperty { Type = JsonObjectType.String, Format = "guid" }, "Payload")(@event);

    [Fact] void should_leave_the_value_unconverted() => _result.ShouldEqual(Value);
    [Fact] void should_log_that_the_value_was_left_unconverted() => LogCalls().Any().ShouldBeTrue();
    [Fact] void should_not_include_the_value_in_the_log() => LogCalls().SelectMany(call => call.GetArguments()).Any(argument => argument?.ToString()?.Contains(Value) == true).ShouldBeFalse();

    IEnumerable<ICall> LogCalls() => _logger.ReceivedCalls().Where(call => call.GetMethodInfo().Name == nameof(ILogger.Log));
}
