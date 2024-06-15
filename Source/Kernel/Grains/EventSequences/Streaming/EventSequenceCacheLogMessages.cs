// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.EventSequences.Streaming;

internal static partial class EventSequenceCacheLogMessages
{
    [LoggerMessage(1, LogLevel.Debug, "Priming event sequence cache from {From} to {To}")]
    internal static partial void Priming(this ILogger<EventSequenceCache> logger, EventSequenceNumber from, EventSequenceNumber to);
}
