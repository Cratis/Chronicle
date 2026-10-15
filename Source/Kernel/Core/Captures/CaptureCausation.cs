// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Holds the well known causation for events ingested by a capture.
/// </summary>
public static class CaptureCausation
{
    /// <summary>
    /// The causation property holding the identifier of the capture.
    /// </summary>
    public const string CaptureId = "captureId";

    /// <summary>
    /// The causation property holding the name of the capture.
    /// </summary>
    public const string CaptureName = "captureName";

    /// <summary>
    /// The causation property holding the inbox event sequence a captured event came from.
    /// </summary>
    public const string SourceSequence = "sourceSequence";

    /// <summary>
    /// The causation property holding the sequence number of the incoming event within the inbox.
    /// </summary>
    public const string SourceSequenceNumber = "sourceSequenceNumber";

    /// <summary>
    /// The causation property holding the event type of the incoming event.
    /// </summary>
    public const string SourceEventType = "sourceEventType";

    /// <summary>
    /// The causation property holding the event source id of the incoming event.
    /// </summary>
    public const string SourceEventSourceId = "sourceEventSourceId";

    /// <summary>
    /// The causation property holding when the incoming event occurred at its origin - the source occurrence,
    /// which is not when the private event was recorded.
    /// </summary>
    public const string SourceOccurred = "sourceOccurred";

    /// <summary>
    /// The <see cref="CausationType"/> for events ingested by a capture.
    /// </summary>
    public static readonly CausationType Type = new("capture");

    /// <summary>
    /// Create the <see cref="Causation"/> for events ingested by a capture.
    /// </summary>
    /// <param name="capture">The <see cref="Capture"/> that ingested the events.</param>
    /// <returns>The <see cref="Causation"/>.</returns>
    public static Causation For(Capture capture) => new(
        DateTimeOffset.UtcNow,
        Type,
        new Dictionary<string, string>
        {
            [CaptureId] = capture.Id.ToString(),
            [CaptureName] = capture.Name
        });

    /// <summary>
    /// Create the <see cref="Causation"/> for private events a capture appended in response to an incoming event.
    /// </summary>
    /// <param name="capture">The <see cref="Capture"/> that translated the event.</param>
    /// <param name="source">The <see cref="EventContext"/> of the incoming event.</param>
    /// <param name="sourceSequence">The inbox <see cref="EventSequenceId"/> the event arrived on.</param>
    /// <returns>The <see cref="Causation"/>, pointing at the inbox event.</returns>
    public static Causation ForSourceEvent(Capture capture, EventContext source, EventSequenceId sourceSequence) => new(
        DateTimeOffset.UtcNow,
        Type,
        new Dictionary<string, string>
        {
            [CaptureId] = capture.Id.ToString(),
            [CaptureName] = capture.Name,
            [SourceSequence] = sourceSequence.Value,
            [SourceSequenceNumber] = source.SequenceNumber.Value.ToString(),
            [SourceEventType] = source.EventType.Id.Value,
            [SourceEventSourceId] = source.EventSourceId.Value,
            [SourceOccurred] = source.Occurred.ToString("O")
        });
}
