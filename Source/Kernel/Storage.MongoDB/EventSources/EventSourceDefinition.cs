// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSources;

namespace Cratis.Chronicle.Storage.MongoDB.EventSources;

/// <summary>
/// Represents the MongoDB document for a registered event source definition.
/// </summary>
public class EventSourceDefinition
{
    /// <summary>
    /// Gets or sets the name of the event source, which is the identifier of the document.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the event source.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the owner of the definition.
    /// </summary>
    public EventSourceOwner Owner { get; set; } = EventSourceOwner.Client;

    /// <summary>
    /// Gets or sets the default concurrency dimensions.
    /// </summary>
    public ConcurrencyDimensions Concurrency { get; set; } = ConcurrencyDimensions.None;

    /// <summary>
    /// Gets or sets the streams of the event source.
    /// </summary>
    public IList<EventStreamDefinition> Streams { get; set; } = [];
}
