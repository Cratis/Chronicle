// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_getting_model_instance;

public class and_processing_fails : given.an_immediate_projection
{
    InvalidOperationException _expected;
    Exception _result;

    void Establish()
    {
        _expected = new InvalidOperationException("Projection processing failed");
        _projection.GetEventTypes().Returns<Task<IEnumerable<EventType>>>(_ => throw _expected);
    }

    async Task Because() => _result = await Catch.Exception(_grain.GetModelInstance);

    [Fact] void should_rethrow_the_processing_failure() => _result.ShouldBeSame(_expected);
    [Fact] void should_log_the_processing_failure()
    {
        var arguments = _logger.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ILogger.Log))
            .Select(call => call.GetArguments())
            .Single(arguments => arguments[0] is LogLevel level && level == LogLevel.Error);

        var eventId = (EventId)arguments[1]!;
        eventId.Id.ShouldEqual(100577318);
        eventId.Name.ShouldEqual("FailedGettingModelInstance");
        arguments[3].ShouldBeSame(_expected);
    }
}
