// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the definition of a read model.
/// </summary>
[ProtoContract]
public class ReadModelDefinition
{
    /// <summary>
    /// Gets or sets the name of the model.
    /// </summary>
    [ProtoMember(1)]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the owner of the read model.
    /// </summary>
    [ProtoMember(2)]
    public ReadModelOwner Owner { get; set; } = ReadModelOwner.None;

    /// <summary>
    /// Gets or sets the generation of the model.
    /// </summary>
    [ProtoMember(3)]
    public ulong Generation { get; set; }

    /// <summary>
    /// Gets or sets the JSON schema for the model.
    /// </summary>
    [ProtoMember(4)]
    public string Schema { get; set; }
}
