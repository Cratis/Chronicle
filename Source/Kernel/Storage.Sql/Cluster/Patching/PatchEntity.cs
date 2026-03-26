// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Patching;

/// <summary>
/// Represents the EF entity for an applied patch.
/// </summary>
public class PatchEntity
{
    /// <summary>
    /// Gets or sets the name of the patch (primary key).
    /// </summary>
    [Key]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version this patch applies to, stored as a string.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the patch was applied.
    /// </summary>
    public DateTimeOffset AppliedAt { get; set; }
}
