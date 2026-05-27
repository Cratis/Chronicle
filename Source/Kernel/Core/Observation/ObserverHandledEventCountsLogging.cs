// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Log messages for <see cref="ObserverHandledEventCounts"/>.
/// </summary>
internal static partial class ObserverHandledEventCountsLogging
{
    [LoggerMessage(LogLevel.Warning, "Failed to refresh observer handled event counts for event store '{EventStore}' namespace '{Namespace}'.")]
    internal static partial void FailedToRefreshHandledEventCounts(
        this ILogger<ObserverHandledEventCounts> logger,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        Exception exception);
}
