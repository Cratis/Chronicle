// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the definition of an index on a read model property.
/// </summary>
[ProtoContract]
public class IndexDefinition
{
    /// <summary>
    /// Gets or sets the property path to index.
    /// </summary>
    /// <remarks>
    /// For nested properties, use dot notation (e.g., "configurations.configurationId").
    /// </remarks>
    [ProtoMember(1)]
    public string PropertyPath { get; set; } = string.Empty;
}
