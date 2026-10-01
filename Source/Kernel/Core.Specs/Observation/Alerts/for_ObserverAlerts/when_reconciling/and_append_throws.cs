// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class and_append_throws : given.an_alert_tracker
{
    Exception _error;

    void Establish() => _sequence.Append(Arg.Any<EventSourceType>(), Arg.Any<EventSourceId>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventType>(), Arg.Any<JsonObject>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Causation>>(), Arg.Any<Identity>(), Arg.Any<IEnumerable<Tag>>(), Arg.Any<ConcurrencyScope>()).Returns(Task.FromException<AppendResult>(new InvalidOperationException("Append failed")));

    async Task Because() => _error = await Catch.Exception(() => _tracker.Reconcile(_snapshot));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_count_the_failure() => _metrics.SumOf("chronicle-alert-transitions-failed").ShouldEqual(1);
    [Fact] void should_log_the_failure() => _logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log) && call.GetArguments()[0] is LogLevel.Error).ShouldEqual(1);
}
