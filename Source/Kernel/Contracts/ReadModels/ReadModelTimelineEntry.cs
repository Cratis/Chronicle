// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents a read model as it stood immediately after one event was applied to it.
/// </summary>
/// <remarks>
/// A snapshot groups the events that happened together, which is what someone reading history wants.
/// A timeline instead has one entry per event, which is what scrubbing through history needs - every
/// step moves by exactly one thing that happened.
/// </remarks>
[ProtoContract]
public class ReadModelTimelineEntry
{
    /// <summary>
    /// Gets or sets the read model as JSON, as it stood after the event was applied.
    /// </summary>
    [ProtoMember(1)]
    public string ReadModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event that produced this state.
    /// </summary>
    [ProtoMember(2)]
    public AppendedEvent? Event { get; set; }
}
