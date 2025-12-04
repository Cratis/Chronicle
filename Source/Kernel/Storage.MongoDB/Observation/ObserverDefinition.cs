// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Represents a MongoDB version of observer definition.
/// </summary>
public class ObserverDefinition
{
    /// <summary>
    /// Gets or sets the <see cref="ObserverId"/> of the observer.
    /// </summary>
    public ObserverId Id { get; set; } = ObserverId.Unspecified;

    /// <summary>
    /// Gets or sets the collection of <see cref="EventType"/> the observer is interested in.
    /// </summary>
    public IDictionary<string, string> EventTypes { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the <see cref="EventSequenceId"/> the observer is associated with.
    /// </summary>
    public EventSequenceId EventSequenceId { get; set; } = EventSequenceId.Unspecified;

    /// <summary>
    /// Gets or sets the <see cref="ObserverType"/> of the observer.
    /// </summary>
    public ObserverType Type { get; set; } = ObserverType.Unknown;

    /// <summary>
    /// Gets or sets the <see cref="ObserverOwner"/> of the observer.
    /// </summary>
    public ObserverOwner Owner { get; set; } = ObserverOwner.None;

    /// <summary>
    /// Gets or sets a value indicating whether the observer supports replay scenarios.
    /// </summary>
    public bool IsReplayable { get; set; }
}
