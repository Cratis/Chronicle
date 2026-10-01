// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Alerts;

internal static partial class ObserverAlertsLogging
{
    [LoggerMessage(LogLevel.Error, "Failed reconciling alert transitions for observer {ObserverKey}; the next reconciliation will retry")]
    internal static partial void ReconciliationFailed(this ILogger<ObserverAlerts> logger, ObserverKey observerKey, Exception exception);

    [LoggerMessage(LogLevel.Error, "Alert transition {Transition} for observer {ObserverKey} was not appended: {Result}; the next reconciliation will retry")]
    internal static partial void TransitionAppendRejected(this ILogger<ObserverAlerts> logger, ObserverKey observerKey, string transition, AppendResult result);
}
