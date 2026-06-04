// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Represents the EF entity for system information (single-row table).
/// </summary>
public class SystemInformationEntity
{
    /// <summary>
    /// Gets or sets the fixed identifier (always 0, single-row table).
    /// The migration creates this column as plain NOT NULL int — not IDENTITY —
    /// so EF must supply the value rather than expect the database to generate it.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the system version, stored as a semantic version string.
    /// </summary>
    public string Version { get; set; } = string.Empty;
}
