// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.EventSequences;

internal static partial class EventSequencesLogMessages
{
    [LoggerMessage(0, LogLevel.Error, "Failed when rehydrating event sequence {EventSequenceId} for event store {EventStore} and namespace {Namespace}")]
    internal static partial void FailedRehydratingEventSequence(this ILogger<EventSequences> logger, EventSequenceId eventSequenceId, EventStoreName eventStore, EventStoreNamespaceName @namespace, Exception exception);
}
