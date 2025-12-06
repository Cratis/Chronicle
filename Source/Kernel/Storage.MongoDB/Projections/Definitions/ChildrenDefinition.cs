// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;

/// <summary>
/// Represents the definition of a children projection.
/// </summary>
public class ChildrenDefinition
{
    /// <summary>
    /// Gets or sets all the <see cref="FromDefinition"/> for string representation of <see cref="EventType">event types</see>.
    /// </summary>
    public IDictionary<string, FromDefinition> From { get; set; } = new Dictionary<string, FromDefinition>();

    /// <summary>
    /// Gets or sets all the <see cref="JoinDefinition"/> for string representation of <see cref="EventType">event types</see>.
    /// </summary>
    public IDictionary<string, JoinDefinition> Join { get; set; } = new Dictionary<string, JoinDefinition>();

    /// <summary>
    /// Gets or sets all the <see cref="ChildrenDefinition"/> for properties on model.
    /// </summary>
    public IDictionary<string, ChildrenDefinition> Children { get; set; } = new Dictionary<string, ChildrenDefinition>();

    /// <summary>
    /// Gets or sets the full <see cref="FromEveryDefinition"/>.
    /// </summary>
    public FromEveryDefinition FromEvery { get; set; } = new FromEveryDefinition();

    /// <summary>
    /// Gets or sets all the <see cref="RemovedWithDefinition"/> for string representation of <see cref="EventType">event types</see>.
    /// </summary>
    public IDictionary<string, RemovedWithDefinition> RemovedWith { get; set; } = new Dictionary<string, RemovedWithDefinition>();

    /// <summary>
    /// Gets or sets all the <see cref="RemovedWithJoinDefinition"/> for string representation of <see cref="EventType">event types</see>.
    /// </summary>
    public IDictionary<string, RemovedWithJoinDefinition> RemovedWithJoin { get; set; } = new Dictionary<string, RemovedWithJoinDefinition>();

    /// <summary>
    /// Gets or sets the optional <see cref="FromEventPropertyDefinition"/> definition.
    /// </summary>
    public FromEventPropertyDefinition? FromEventProperty { get; set; }

    /// <summary>
    /// Gets or sets the property on model that identifies the unique object, typically the key - or id (event source id).
    /// </summary>
    public required string IdentifiedBy { get; set; }
}
