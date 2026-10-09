// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Holds the well known <see cref="Tag">tags</see> stamped on events ingested by a capture,
/// making captured events queryable by capture.
/// </summary>
public static class CaptureTags
{
    /// <summary>
    /// The tag every captured event carries.
    /// </summary>
    public static readonly Tag Capture = new("Capture");

    /// <summary>
    /// Get the tags for events ingested by a capture - the <see cref="Capture"/> tag and the capture's name.
    /// </summary>
    /// <param name="name">The <see cref="CaptureName"/> of the capture.</param>
    /// <returns>The tags.</returns>
    public static IEnumerable<Tag> For(CaptureName name) => [Capture, new Tag(name)];

    /// <summary>
    /// Gets the tag that marks events a capture appended in response to one specific incoming event. It is what makes
    /// capturing idempotent: an incoming event that already has this tag in the event log has already been translated.
    /// </summary>
    /// <param name="captureId">The <see cref="CaptureId"/> of the capture.</param>
    /// <param name="sourceSequence">The inbox <see cref="EventSequenceId"/> the incoming event arrived on.</param>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> of the incoming event.</param>
    /// <returns>The <see cref="Tag"/>.</returns>
    public static Tag ForSourceEvent(CaptureId captureId, EventSequenceId sourceSequence, EventSequenceNumber sequenceNumber) =>
        new($"capture-source:{captureId.Value:N}:{sourceSequence.Value}:{sequenceNumber.Value}");
}
