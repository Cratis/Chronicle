// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name

internal static partial class ObserverRemoverLogMessages
{
    [LoggerMessage(LogLevel.Information, "Removing observer {ObserverId} from event store {EventStore} by operator request")]
    internal static partial void RemovingObserver(this ILogger<ObserverRemover> logger, ObserverId observerId, EventStoreName eventStore);

    [LoggerMessage(LogLevel.Information, "Removed observer {ObserverId} from event store {EventStore}")]
    internal static partial void RemovedObserver(this ILogger<ObserverRemover> logger, ObserverId observerId, EventStoreName eventStore);

    [LoggerMessage(LogLevel.Information, "Refusing to remove observer {ObserverId} - {Outcome} in namespace {Namespace}")]
    internal static partial void RefusingObserverRemoval(this ILogger<ObserverRemover> logger, ObserverId observerId, ObserverRemovalOutcome outcome, EventStoreNamespaceName @namespace);
}
