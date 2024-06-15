// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the from definition of a projection.
/// </summary>
[ProtoContract]
public class JoinDefinition
{
    /// <summary>
    /// Gets or sets the property representing the model property one is joining on.
    /// </summary>
    [ProtoMember(1)]
    public string On { get; set; }

    /// <summary>
    /// Gets or sets the properties and expressions for each property.
    /// </summary>
    [ProtoMember(2)]
    public IDictionary<string, string> Properties { get; set; }

    /// <summary>
    /// Gets or sets the key expression, represents the key to use for identifying the model instance.
    /// </summary>
    [ProtoMember(3)]
    public string Key { get; set; }
}
