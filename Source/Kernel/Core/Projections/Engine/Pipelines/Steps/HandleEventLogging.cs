// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

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

    [LoggerMessage(LogLevel.Debug, "Processing child projection '{ChildPath}' for event type '{EventType}' at sequence {SequenceNumber}")]
    internal static partial void ProcessingChildProjection(this ILogger<HandleEvent> logger, string childPath, EventTypeId eventType, ulong sequenceNumber);

    [LoggerMessage(LogLevel.Debug, "Child projection '{ChildPath}' has key resolver for event type '{EventType}', resolved key='{Key}'")]
    internal static partial void ChildHasKeyResolver(this ILogger<HandleEvent> logger, string childPath, EventTypeId eventType, object? key);

    [LoggerMessage(LogLevel.Debug, "Child projection '{ChildPath}' does NOT have key resolver for event type '{EventType}', passing context through")]
    internal static partial void ChildNoKeyResolver(this ILogger<HandleEvent> logger, string childPath, EventTypeId eventType);

    [LoggerMessage(LogLevel.Debug, "Child projection '{ChildPath}' key resolution deferred for event type '{EventType}' at sequence {SequenceNumber} - creating future")]
    internal static partial void ChildKeyResolutionDeferred(this ILogger<HandleEvent> logger, string childPath, EventTypeId eventType, ulong sequenceNumber);
}
