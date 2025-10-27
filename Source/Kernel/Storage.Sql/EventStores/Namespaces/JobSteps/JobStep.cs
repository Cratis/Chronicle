// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Chronicle.Concepts.Jobs;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.JobSteps;

/// <summary>
/// Represents the entity for job steps.
/// </summary>
[Index(nameof(JobId))]
[Index(nameof(Status))]
[Index(nameof(Type))]
public class JobStep
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the job identifier this step belongs to.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Gets or sets the job step identifier.
    /// </summary>
    public Guid JobStepId { get; set; }

    /// <summary>
    /// Gets or sets the job step type.
    /// </summary>
    [MaxLength(256)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the job step name.
    /// </summary>
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the job step status.
    /// </summary>
    public JobStepStatus Status { get; set; }

    /// <summary>
    /// Gets or sets whether the job step has been prepared.
    /// </summary>
    public bool IsPrepared { get; set; }

    /// <summary>
    /// Gets or sets the serialized job step state as JSON.
    /// </summary>
    public string StateJson { get; set; } = string.Empty;
}
