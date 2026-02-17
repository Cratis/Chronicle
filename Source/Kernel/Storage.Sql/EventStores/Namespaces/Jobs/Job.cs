// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Chronicle.Concepts.Jobs;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Jobs;

/// <summary>
/// Represents the entity for jobs.
/// </summary>
[Index(nameof(Status))]
[Index(nameof(Type))]
public class Job
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the job type.
    /// </summary>
    [MaxLength(256)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the job status.
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the time that this job was created.
    /// </summary>
    public DateTimeOffset Created { get; set; }

    /// <summary>
    /// Gets or sets the serialized job state as JSON.
    /// </summary>
    public string StateJson { get; set; } = string.Empty;
}
