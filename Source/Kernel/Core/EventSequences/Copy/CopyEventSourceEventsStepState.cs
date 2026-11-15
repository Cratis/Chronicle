// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.EventSequences.Copy;

/// <summary>
/// Represents the persisted state of a <see cref="CopyEventSourceEventsStep"/>.
/// </summary>
public class CopyEventSourceEventsStepState : JobStepState
{
    /// <summary>
    /// Gets or sets the event sequence holding both event sources.
    /// </summary>
    public EventSequenceId EventSequenceId { get; set; } = EventSequenceId.Unspecified;

    /// <summary>
    /// Gets or sets the event source to copy from.
    /// </summary>
    public EventSourceId SourceId { get; set; } = EventSourceId.Unspecified;

    /// <summary>
    /// Gets or sets the event source to copy to.
    /// </summary>
    public EventSourceId TargetId { get; set; } = EventSourceId.Unspecified;

    /// <summary>
    /// Gets or sets the event types to copy. Empty means every type.
    /// </summary>
    public IList<EventTypeId> EventTypes { get; set; } = [];
}
