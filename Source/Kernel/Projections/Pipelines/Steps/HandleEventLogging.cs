// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Pipelines.Steps;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class HandleEventLogging
{
    [LoggerMessage(LogLevel.Debug, "Handling event with sequence number {SequenceNumber}")]
    internal static partial void HandlingEvent(this ILogger<HandleEvent> logger, ulong sequenceNumber);

    [LoggerMessage(LogLevel.Trace, "Projection '{Id} - {Path}' is not accepting event of type '{EventType}' at sequence number {SequenceNumber}")]
    internal static partial void EventNotAccepted(this ILogger<HandleEvent> logger, ulong sequenceNumber, ProjectionId id, ProjectionPath path, EventType eventType);

    [LoggerMessage(LogLevel.Trace, "Projecting for event with sequence number {SequenceNumber}")]
    internal static partial void Projecting(this ILogger<HandleEvent> logger, ulong sequenceNumber);
}
