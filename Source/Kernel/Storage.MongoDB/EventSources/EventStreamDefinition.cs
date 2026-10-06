// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSources;

namespace Cratis.Chronicle.Storage.MongoDB.EventSources;

/// <summary>
/// Represents the MongoDB representation of an event stream of an event source.
/// </summary>
public class EventStreamDefinition
{
    /// <summary>
    /// Gets or sets the name of the stream.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the stream.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the concurrency dimensions of the stream.
    /// </summary>
    public ConcurrencyDimensions Concurrency { get; set; } = ConcurrencyDimensions.None;
}
