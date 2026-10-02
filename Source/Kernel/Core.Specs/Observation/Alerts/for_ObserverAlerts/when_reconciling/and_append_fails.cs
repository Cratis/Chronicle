// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class and_append_fails : given.an_alert_tracker
{
    Exception _error;

    void Establish() => AppendReturns(AppendResult.Failed(CorrelationId.NotSet, (AppendError[])[new("Storage unavailable")]));

    async Task Because() => _error = await Catch.Exception(() => _tracker.Reconcile(_snapshot));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_count_the_failure() => _metrics.SumOf("chronicle-alert-transitions-failed").ShouldEqual(1);
    [Fact] void should_log_the_failure() => _logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log) && call.GetArguments()[0] is LogLevel.Error).ShouldEqual(1);
    [Fact] void should_have_no_metric_unit() => _metrics.InstrumentNamed("chronicle-alert-transitions-failed").Unit.ShouldBeNull();
}
