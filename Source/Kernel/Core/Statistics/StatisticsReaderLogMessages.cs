// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Statistics;

internal static partial class StatisticsReaderLogMessages
{
    /// <summary>
    /// Logs that a namespace cannot contribute complete statistics within the row bound.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="maximumRows">The row bound.</param>
    /// <param name="returnedRows">The number of returned rows.</param>
    /// <param name="totalCount">The reported total count.</param>
    [LoggerMessage(LogLevel.Warning, "Statistics read for event store '{EventStore}', namespace '{Namespace}' reached its {MaximumRows} row limit ({ReturnedRows} returned, {TotalCount} total). Refusing incomplete totals")]
    internal static partial void StatisticsReadLimitReached(this ILogger<StatisticsReader> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, int maximumRows, int returnedRows, long totalCount);
}
