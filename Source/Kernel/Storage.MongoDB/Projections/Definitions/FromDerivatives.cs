// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;

/// <summary>
/// Represents the definition from for a set of events.
/// </summary>
/// <remarks>
/// This is typically representing event types that are deriving from a common base type.
/// </remarks>
public class FromDerivatives
{
    /// <summary>
    /// Gets or sets the collection of <see cref="EventType"/> for the definition.
    /// </summary>
    public IEnumerable<EventType> EventTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the from definition associated.
    /// </summary>
    public FromDefinition From { get; set; } = new FromDefinition();
}
