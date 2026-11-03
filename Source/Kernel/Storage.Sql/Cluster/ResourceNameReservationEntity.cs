// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Represents a reserved resource name key and the owner holding it.
/// </summary>
[Table(WellKnownTableNames.ResourceNameReservations)]
public class ResourceNameReservationEntity
{
    /// <summary>
    /// Gets or sets the reserved key.
    /// </summary>
    [Key]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the owner, as the exact names it was created with.
    /// </summary>
    public string Owner { get; set; } = string.Empty;
}
