// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents a draft read model definition that can be used for preview/save operations
/// before the read model type is actually created.
/// </summary>
[ProtoContract]
public class DraftReadModelDefinition
{
    /// <summary>
    /// Gets or sets the name of the read model.
    /// </summary>
    [ProtoMember(1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON schema for the read model.
    /// </summary>
    [ProtoMember(2)]
    public string Schema { get; set; } = string.Empty;
}
