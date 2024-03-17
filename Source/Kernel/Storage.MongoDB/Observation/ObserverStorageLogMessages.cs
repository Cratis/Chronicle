// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Kernel.Storage.MongoDB.Observation;

internal static partial class ObserverStorageLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Getting tail sequence number for observer {ObserverId} for namespace {Namespace} in event store {EventStore}")]
    internal static partial void GettingTailSequenceNumber(this ILogger<ObserverStorage> logger, ObserverId observerId, EventStoreNamespaceName @namespace, EventStoreName eventStore);

    [LoggerMessage(1, LogLevel.Trace, "Got tail sequence number {TailSequenceNumber} for observer {ObserverId} for namespace {Namespace} in event store {EventStore}")]
    internal static partial void GotTailSequenceNumber(this ILogger<ObserverStorage> logger, ObserverId observerId, EventStoreNamespaceName @namespace, EventStoreName eventStore, EventSequenceNumber tailSequenceNumber);

    [LoggerMessage(2, LogLevel.Trace, "Getting next sequence number greater or equal than {SequenceNumber} for observer {ObserverId} for namespace {Namespace} in event store {EventStore}")]
    internal static partial void GettingNextSequenceNumberGreaterOrEqualThan(this ILogger<ObserverStorage> logger, ObserverId observerId, EventStoreNamespaceName @namespace, EventStoreName eventStore, EventSequenceNumber sequenceNumber);

    [LoggerMessage(3, LogLevel.Trace, "Got next sequence number {NextSequenceNumber} for observer {ObserverId} for namespace {Namespace} in event store {EventStore}")]
    internal static partial void GotNextSequenceNumber(this ILogger<ObserverStorage> logger, ObserverId observerId, EventStoreNamespaceName @namespace, EventStoreName eventStore, EventSequenceNumber nextSequenceNumber);
}
