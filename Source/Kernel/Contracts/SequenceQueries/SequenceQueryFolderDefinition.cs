// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.SequenceQueries;

/// <summary>
/// Represents a folder in the saved event sequence query hierarchy.
/// </summary>
[ProtoContract]
public class SequenceQueryFolderDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier of the folder.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets who the folder is visible to.
    /// </summary>
    [ProtoMember(2)]
    public SequenceQueryScope Scope { get; set; }

    /// <summary>
    /// Gets or sets the identity that created the folder.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace the folder belongs to.
    /// </summary>
    [ProtoMember(4, IsRequired = true)]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets where the folder sits within its scope. Nested folders are separated by a
    /// forward slash.
    /// </summary>
    [ProtoMember(5, IsRequired = true)]
    public string Path { get; set; } = string.Empty;
}
