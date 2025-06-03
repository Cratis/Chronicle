// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Jobs;

/// <summary>
/// Represents the state of a job.
/// </summary>
/// <param name="Id">The unique identifier for the job.</param>
/// <param name="Details">The details for a job.</param>
/// <param name="Type">The type of the job.</param>
/// <param name="Status">The status of the job.</param>
/// <param name="Created">When job was created.</param>
/// <param name="StatusChanges">Collection of status changes that happened to the job.</param>
/// <param name="Progress">The <see cref="JobProgress"/>.</param>
public record Job(
    Guid Id,
    string Details,
    string Type,
    JobStatus Status,
    DateTimeOffset Created,
    IEnumerable<JobStatusChanged> StatusChanges,
    JobProgress Progress);
