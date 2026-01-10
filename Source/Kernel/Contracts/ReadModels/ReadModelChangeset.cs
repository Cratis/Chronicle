// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents a changeset for a read model.
/// </summary>
[ProtoContract]
public class ReadModelChangeset
{
    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    [ProtoMember(1)]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model key.
    /// </summary>
    [ProtoMember(2)]
    public string ModelKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the read model as JSON.
    /// </summary>
    [ProtoMember(3)]
    public string ReadModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the read model was removed.
    /// </summary>
    [ProtoMember(4)]
    public bool Removed { get; set; }
}
