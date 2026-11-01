// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name

internal static partial class ObserverSequenceHandoverLogMessages
{
    [LoggerMessage(LogLevel.Information, "Observer '{ObserverId}' handed over from event sequence '{From}' to '{To}'")]
    internal static partial void HandedOverToEventSequence(this ILogger<Observer> logger, ObserverId observerId, EventSequenceId from, EventSequenceId to);

    [LoggerMessage(LogLevel.Debug, "Observer '{ObserverId}' changed owner while handing over to event sequence '{To}'; attempt {Attempt}")]
    internal static partial void HandOverOwnerChanged(this ILogger<Observer> logger, ObserverId observerId, EventSequenceId to, int attempt);

    [LoggerMessage(LogLevel.Warning, "Observer '{ObserverId}' could not be handed over to event sequence '{To}' because its stored owner kept changing. The next subscription retries")]
    internal static partial void HandOverGaveUp(this ILogger<Observer> logger, ObserverId observerId, EventSequenceId to);
}
