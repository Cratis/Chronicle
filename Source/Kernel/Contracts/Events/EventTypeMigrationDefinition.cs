// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Events;

/// <summary>
/// Represents a migration definition between two generations of an event type.
/// </summary>
[ProtoContract]
public class EventTypeMigrationDefinition
{
    /// <summary>
    /// Gets or sets the generation to migrate from.
    /// </summary>
    [ProtoMember(1)]
    public uint FromGeneration { get; set; }

    /// <summary>
    /// Gets or sets the generation to migrate to.
    /// </summary>
    [ProtoMember(2)]
    public uint ToGeneration { get; set; }

    /// <summary>
    /// Gets or sets the collection of migration operations.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public IList<EventTypeMigrationOperation> Operations { get; set; } = [];

    /// <summary>
    /// Gets or sets the JmesPath expression for the migration as JSON.
    /// </summary>
    [ProtoMember(4)]
    public string JmesPath { get; set; } = string.Empty;
}
