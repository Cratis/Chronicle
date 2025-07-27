// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the definition of what removes an element in a child relationship through a join.
/// </summary>
[ProtoContract]
public class RemovedWithJoinDefinition
{
    /// <summary>
    /// Gets or sets the key expression, represents the key to use for identifying the model instance.
    /// </summary>
    [ProtoMember(1)]
    public string Key { get; set; }
}
