// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Represents the state of a job.
/// </summary>
/// <param name="eventStore">The <see cref="IEventStore"/>.</param>
public class Job(IEventStore eventStore)
{
    /// <summary>
    /// Gets or sets the unique identifier for the job.
    /// </summary>
    public JobId Id { get; set; } = JobId.NotSet;

    /// <summary>
    /// Gets or sets the details for a job.
    /// </summary>
    public JobDetails Details { get; set; } = JobDetails.NotSet;

    /// <summary>
    /// Gets or sets the type of the job.
    /// </summary>
    public JobType Type { get; set; } = JobType.NotSet;

    /// <summary>
    /// Gets or sets the status of the job.
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// Gets or sets when job was created.
    /// </summary>
    public DateTimeOffset Created { get; set; }

    /// <summary>
    /// Gets or sets collection of status changes that happened to the job.
    /// </summary>
    public IEnumerable<JobStatusChanged> StatusChanges { get; set; } = [];

    /// <summary>
    /// Gets or sets the <see cref="JobProgress"/>.
    /// </summary>
    public JobProgress Progress { get; set; } = new();

    /// <summary>
    /// Get the job steps for the job.
    /// </summary>
    /// <param name="statuses">The <see cref="JobStepStatus"/> to filter by. If no statuses are specified, it will return for all statuses.</param>
    /// <returns>Collection of <see cref="JobStep"/>.</returns>
    public async Task<IEnumerable<JobStep>> GetJobSteps(params JobStepStatus[] statuses)
    {
        var servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;
        var result = await servicesAccessor.Services.Jobs.GetJobSteps(new()
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            JobId = Id,
            Statuses = statuses.Select(status => (Contracts.Jobs.JobStepStatus)(int)status).ToArray()
        });

        return result.ToClient();
    }
}
