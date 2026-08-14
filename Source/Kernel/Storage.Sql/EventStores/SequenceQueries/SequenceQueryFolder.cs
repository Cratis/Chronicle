// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Chronicle.Concepts.SequenceQueries;

namespace Cratis.Chronicle.Storage.Sql.EventStores.SequenceQueries;

/// <summary>
/// Represents a folder in the saved query hierarchy.
/// </summary>
public class SequenceQueryFolder
{
    /// <summary>
    /// Gets or sets the unique identifier of the folder.
    /// </summary>
    [Key]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets who the folder is visible to.
    /// </summary>
    public SequenceQueryScope Scope { get; set; }

    /// <summary>
    /// Gets or sets the identity that created the folder.
    /// </summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace the folder belongs to.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets where the folder sits within its scope. Nested folders are separated by a
    /// forward slash.
    /// </summary>
    public string Path { get; set; } = string.Empty;
}
