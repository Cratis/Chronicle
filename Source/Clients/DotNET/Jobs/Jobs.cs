// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobs"/>.
/// </summary>
/// <param name="eventStore">The <see cref="IEventStore"/>.</param>
public class Jobs(IEventStore eventStore) : IJobs
{
    readonly IChronicleServicesAccessor _servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;

    /// <inheritdoc/>
    public Task Stop(JobId jobId) =>
        _servicesAccessor.Services.Jobs.Stop(new()
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            JobId = jobId
        });

    /// <inheritdoc/>
    public Task Resume(JobId jobId) =>
        _servicesAccessor.Services.Jobs.Resume(new()
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            JobId = jobId
        });

    /// <inheritdoc/>
    public Task Delete(JobId jobId) =>
        _servicesAccessor.Services.Jobs.Delete(new()
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            JobId = jobId
        });

    /// <inheritdoc/>
    public async Task<Job?> GetJob(JobId jobId)
    {
        var result = await _servicesAccessor.Services.Jobs.GetJob(new()
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            JobId = jobId
        });

        return result.Value switch
        {
            Contracts.Jobs.Job job => job.ToClient(eventStore),
            Contracts.Jobs.JobError => null,
            _ => null
        };
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Job>> GetJobs()
    {
        var jobs = await _servicesAccessor.Services.Jobs.GetJobs(new()
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
        });
        return jobs.ToClient(eventStore);
    }
}
