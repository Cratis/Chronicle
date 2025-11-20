// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Events;

/// <summary>
/// Represents a specific generation of an event type.
/// </summary>
[ProtoContract]
public class EventTypeGenerationDefinition
{
    /// <summary>
    /// Gets or sets the generation number.
    /// </summary>
    [ProtoMember(1)]
    public uint Generation { get; set; }

    /// <summary>
    /// Gets or sets the JSON schema for this generation.
    /// </summary>
    [ProtoMember(2)]
    public string Schema { get; set; } = string.Empty;
}
