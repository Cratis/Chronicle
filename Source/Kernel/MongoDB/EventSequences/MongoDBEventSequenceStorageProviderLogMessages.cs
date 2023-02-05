// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.MongoDB;
internal static partial class MongoDBEventSequenceStorageProviderLogMessages
{
    [LoggerMessage(0, LogLevel.Debug, "Getting head sequence number for event sequence {EventSequenceId}")]
    internal static partial void GettingHeadSequenceNumber(this ILogger<MongoDBEventSequenceStorageProvider> logger, EventSequenceId eventSequenceId);

    [LoggerMessage(1, LogLevel.Debug, "Getting tail sequence number for event sequence {EventSequenceId}")]
    internal static partial void GettingTailSequenceNumber(this ILogger<MongoDBEventSequenceStorageProvider> logger, EventSequenceId eventSequenceId);

    [LoggerMessage(2, LogLevel.Debug, "Getting last instance of event of type '{EventTypeId}' and event source id '{EventSourceId}' in event sequence {EventSequenceId}")]
    internal static partial void GettingLastInstanceFor(this ILogger<MongoDBEventSequenceStorageProvider> logger, EventSequenceId eventSequenceId, EventTypeId eventTypeId, EventSourceId eventSourceId);

    [LoggerMessage(3, LogLevel.Debug, "Getting last instance of event of types ['{EventTypes}'] and event source id '{EventSourceId}' in event sequence {EventSequenceId}")]
    internal static partial void GettingLastInstanceOfAny(this ILogger<MongoDBEventSequenceStorageProvider> logger, EventSequenceId eventSequenceId, EventSourceId eventSourceId, IEnumerable<EventTypeId> eventTypes);

    [LoggerMessage(4, LogLevel.Debug, "Getting events from sequence number {From} in event sequence {EventSequenceId}")]
    internal static partial void GettingFromSequenceNumber(this ILogger<MongoDBEventSequenceStorageProvider> logger, EventSequenceId eventSequenceId, EventSequenceNumber from);

    [LoggerMessage(5, LogLevel.Debug, "Getting range of events from {From} to {To} in event sequence {EventSequenceId}")]
    internal static partial void GettingRange(this ILogger<MongoDBEventSequenceStorageProvider> logger, EventSequenceId eventSequenceId, EventSequenceNumber from, EventSequenceNumber to);
}
