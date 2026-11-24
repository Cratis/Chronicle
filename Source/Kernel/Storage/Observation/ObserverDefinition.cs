// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Represents the definition of an observer.
/// </summary>
/// <param name="Identifier">The <see cref="ObserverId"/> representing the observer uniquely.</param>
/// <param name="EventTypes">The list of <see cref="EventTypeId"/> representing the event types the observer is interested in.</param>
/// <param name="EventSequenceId">The <see cref="EventSequenceId"/> representing the current event sequence the observer is on.</param>
/// <param name="Type">The <see cref="ObserverType"/> representing the type of the observer.</param>
/// <param name="Owner">The <see cref="ObserverOwner"/> representing the owner of the observer.</param>
/// <param name="IsReplayable">A boolean indicating whether the observer is replayable.</param>
[Immutable]
[GenerateSerializer]
[Alias("Cratis.Chronicle.Storage.Observation.ObserverDefinition")]
public record ObserverDefinition(
    [Id(0)] ObserverId Identifier,
    [Id(1)] IEnumerable<EventType> EventTypes,
    [Id(2)] EventSequenceId EventSequenceId,
    [Id(3)] ObserverType Type,
    [Id(4)] ObserverOwner Owner,
    [Id(5)] bool IsReplayable)
{
    /// <summary>
    /// Represents an empty observer definition.
    /// </summary>
    public static readonly ObserverDefinition Empty = new(ObserverId.Unspecified, [], EventSequenceId.Unspecified, ObserverType.Unknown, ObserverOwner.None, false);

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverDefinition"/> class.
    /// </summary>
    public ObserverDefinition()
        : this(ObserverId.Unspecified, [], EventSequenceId.Unspecified, ObserverType.Unknown, ObserverOwner.None, false)
    {
    }

    /// <summary>
    /// Gets the event generation delivery policy.
    /// </summary>
    [Id(6)]
    public EventGenerationDelivery GenerationDelivery { get; init; } = EventGenerationDelivery.Compatibility;

    /// <summary>
    /// Gets the <see cref="ObserverDefinitionRevision"/> of the definition, which increases every time its content changes.
    /// </summary>
    /// <remarks>
    /// Stamped by storage when the definition is saved, never by the code that builds a definition. A definition stored before
    /// revisions existed reads as <see cref="ObserverDefinitionRevision.None"/>.
    /// </remarks>
    [Id(7)]
    public ObserverDefinitionRevision Revision { get; init; } = ObserverDefinitionRevision.None;

    /// <summary>
    /// Gets whether another definition has the same content as this one, whatever the revisions of the two are.
    /// </summary>
    /// <param name="other">The <see cref="ObserverDefinition"/> to compare with.</param>
    /// <returns>True when the content is the same; otherwise false.</returns>
    public bool HasSameContentAs(ObserverDefinition other) =>
        (this with { EventTypes = EventTypeSet.From(EventTypes), Revision = ObserverDefinitionRevision.None })
            .Equals(other with { EventTypes = EventTypeSet.From(other.EventTypes), Revision = ObserverDefinitionRevision.None });
}
