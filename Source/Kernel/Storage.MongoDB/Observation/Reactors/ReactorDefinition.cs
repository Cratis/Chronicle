// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Reactors;

/// <summary>
/// Represents the registration of a single client observer.
/// </summary>
public class ReactorDefinition
{
    /// <summary>
    /// Gets or sets the <see cref="ReactorId"/> of the reactor.
    /// </summary>
    public ReactorId Id { get; set; } = ReactorId.Unspecified;

    /// <summary>
    /// Gets or sets the owner of the reactor.
    /// </summary>
    public ReactorOwner Owner { get; set; } = ReactorOwner.Unspecified;

    /// <summary>
    /// Gets or sets the <see cref="EventSequenceId"/> the reactor is for.
    /// </summary>
    public EventSequenceId EventSequenceId { get; set; } = EventSequenceId.Unspecified;

    /// <summary>
    /// Gets or sets the event types and key expressions that the reactor subscribes to.
    /// </summary>
    public IDictionary<string, string> EventTypes { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets a value indicating whether the reactor is replayable.
    /// </summary>
    public bool IsReplayable { get; set; } = true;

    /// <summary>
    /// Gets or sets the categories the reactor belongs to.
    /// </summary>
    public IEnumerable<string> Categories { get; set; } = [];
}
