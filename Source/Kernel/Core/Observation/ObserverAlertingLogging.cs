// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

internal static partial class ObserverAlertingLogging
{
    [LoggerMessage(LogLevel.Error, "Failed reporting alert state for observer {ObserverKey}; observation will continue")]
    internal static partial void AlertStateReportingFailed(this ILogger<Observer> logger, ObserverKey observerKey, Exception exception);
}
