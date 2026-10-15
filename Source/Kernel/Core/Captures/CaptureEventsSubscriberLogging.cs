// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Log messages for <see cref="CaptureEventsSubscriber"/>.
/// </summary>
internal static partial class CaptureEventsSubscriberLogging
{
    [LoggerMessage(LogLevel.Error, "Capturing events for observer '{ObserverId}' from '{EventSequenceId}' in {EventStore}/{Namespace} failed")]
    internal static partial void CapturingEventsFailed(this ILogger<CaptureEventsSubscriber> logger, Exception exception, ObserverId observerId, EventSequenceId eventSequenceId, EventStoreName eventStore, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Information, "Capture '{Name}' has already translated the event at sequence number {SequenceNumber} of '{EventSequenceId}' - not appending again")]
    internal static partial void SkippingAlreadyCapturedEvent(this ILogger<CaptureEventsSubscriber> logger, CaptureName name, EventSequenceNumber sequenceNumber, EventSequenceId eventSequenceId);
}
