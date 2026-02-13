// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the type of a read model.
/// </summary>
[ProtoContract]
public class ReadModelType
{
    /// <summary>
    /// Gets or sets the unique identifier of the read model type.
    /// </summary>
    [ProtoMember(1)]
    public string Identifier { get; set; }

    /// <summary>
    /// Gets or sets the generation of the read model type.
    /// </summary>
    [ProtoMember(2)]
    public uint Generation { get; set; }
}
