// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Log messages for <see cref="ProjectionObserverSubscriber"/>.
/// </summary>
internal static partial class ProjectionObserverSubscriberLogging
{
    [LoggerMessage(LogLevel.Warning, "Projection pipeline for key {Key} is disconnected")]
    internal static partial void PipelineDisconnected(this ILogger<ProjectionObserverSubscriber> logger, ObserverSubscriberKey key);

    [LoggerMessage(LogLevel.Warning, "An error occurred while handling to projection pipeline for key {Key}. Last successfully observed event was {LastObservedEventSequenceNumber}")]
    internal static partial void ErrorHandling(this ILogger<ProjectionObserverSubscriber> logger, Exception ex, ObserverSubscriberKey key, ulong lastObservedEventSequenceNumber);

    [LoggerMessage(LogLevel.Trace, "Successfully handled event {EventSequenceNumber} for projection pipeline for key {Key}")]
    internal static partial void SuccessfullyHandledEvent(this ILogger<ProjectionObserverSubscriber> logger, ulong eventSequenceNumber, ObserverSubscriberKey key);

    [LoggerMessage(LogLevel.Trace, "Successfully handled all events for projection pipeline for key {Key}")]
    internal static partial void SuccessfullyHandledAllEvents(this ILogger<ProjectionObserverSubscriber> logger, ObserverSubscriberKey key);
}
