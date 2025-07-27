// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the definition of what removes an element in a child relationship.
/// </summary>
[ProtoContract]
public class RemovedWithDefinition
{
    /// <summary>
    /// Gets or sets the key expression, represents the key to use for identifying the model instance.
    /// </summary>
    [ProtoMember(1)]
    public string Key { get; set; }

    /// <summary>
    /// Gets or sets the optional parent key expression, typically used in child relationships for identifying parent model.
    /// </summary>
    [ProtoMember(2)]
    public string? ParentKey { get; set; }
}
