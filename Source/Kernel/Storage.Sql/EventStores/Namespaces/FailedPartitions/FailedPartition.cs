// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions;

/// <summary>
/// Represents the entity for failed partitions.
/// </summary>
[Index(nameof(ObserverId))]
[Index(nameof(Partition))]
[Index(nameof(IsResolved))]
public class FailedPartition
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the partition that is failed.
    /// </summary>
    [MaxLength(1024)]
    public string Partition { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the observer identifier for which this is a failed partition.
    /// </summary>
    [MaxLength(256)]
    public string ObserverId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the failure is resolved.
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// Gets or sets the serialized failed partition state as JSON.
    /// </summary>
    public string StateJson { get; set; } = string.Empty;
}
