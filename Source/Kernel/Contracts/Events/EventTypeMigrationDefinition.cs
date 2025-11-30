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
    /// Gets or sets the JmesPath expression for upcast migration (From → To) as JSON.
    /// </summary>
    [ProtoMember(4)]
    public string UpcastJmesPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JmesPath expression for downcast migration (To → From) as JSON.
    /// </summary>
    [ProtoMember(5)]
    public string DowncastJmesPath { get; set; } = string.Empty;
}
