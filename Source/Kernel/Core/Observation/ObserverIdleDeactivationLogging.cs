// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

internal static partial class ObserverIdleDeactivationLogging
{
    [LoggerMessage(LogLevel.Debug, "Releasing observer {ObserverId} in event store {EventStore} and namespace {Namespace}: it has been idle without a subscription for {IdlePeriod}")]
    internal static partial void ReleasingIdleObserver(this ILogger<Observer> logger, ObserverId observerId, EventStoreName eventStore, EventStoreNamespaceName @namespace, TimeSpan idlePeriod);
}
