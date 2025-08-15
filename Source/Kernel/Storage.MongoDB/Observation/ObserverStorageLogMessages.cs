// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

internal static partial class ObserverStorageLogMessages
{
    [LoggerMessage(LogLevel.Trace, "Getting tail sequence number for observer {ObserverId} for namespace {Namespace} in event store {EventStore}")]
    internal static partial void GettingTailSequenceNumber(this ILogger<ObserverStateStorage> logger, ObserverId observerId, EventStoreNamespaceName @namespace, EventStoreName eventStore);

    [LoggerMessage(LogLevel.Trace, "Got tail sequence number {TailSequenceNumber} for observer {ObserverId} for namespace {Namespace} in event store {EventStore}")]
    internal static partial void GotTailSequenceNumber(this ILogger<ObserverStateStorage> logger, ObserverId observerId, EventStoreNamespaceName @namespace, EventStoreName eventStore, EventSequenceNumber tailSequenceNumber);

    [LoggerMessage(LogLevel.Trace, "Getting next sequence number greater or equal than {SequenceNumber} for observer {ObserverId} for namespace {Namespace} in event store {EventStore}")]
    internal static partial void GettingNextSequenceNumberGreaterOrEqualThan(this ILogger<ObserverStateStorage> logger, ObserverId observerId, EventStoreNamespaceName @namespace, EventStoreName eventStore, EventSequenceNumber sequenceNumber);

    [LoggerMessage(LogLevel.Trace, "Got next sequence number {NextSequenceNumber} for observer {ObserverId} for namespace {Namespace} in event store {EventStore}")]
    internal static partial void GotNextSequenceNumber(this ILogger<ObserverStateStorage> logger, ObserverId observerId, EventStoreNamespaceName @namespace, EventStoreName eventStore, EventSequenceNumber nextSequenceNumber);
}
