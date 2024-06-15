// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Models;

/// <summary>
/// Represents the definition of a model.
/// </summary>
[ProtoContract]
public class ModelDefinition
{
    /// <summary>
    /// Gets or sets the name of the model.
    /// </summary>
    [ProtoMember(1)]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the JSON schema for the model.
    /// </summary>
    [ProtoMember(2)]
    public string Schema { get; set; }
}
