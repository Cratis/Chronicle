// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobs"/>.
/// </summary>
/// <param name="eventStoreName">Name of the event store.</param>
/// <param name="namespace">Namespace for the event store.</param>
/// <param name="connection"><see cref="IChronicleConnection"/> for working with the connection to Chronicle.</param>
public class Jobs(
    EventStoreName eventStoreName,
    EventStoreNamespaceName @namespace,
    IChronicleConnection connection) : IJobs
{
    /// <inheritdoc/>
    public Task Stop(JobId jobId) =>
        connection.Services.Jobs.Stop(new()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            JobId = jobId
        });

    /// <inheritdoc/>
    public Task Resume(JobId jobId) =>
        connection.Services.Jobs.Resume(new()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            JobId = jobId
        });

    /// <inheritdoc/>
    public Task Delete(JobId jobId) =>
        connection.Services.Jobs.Delete(new()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            JobId = jobId
        });

    /// <inheritdoc/>
    public async Task<Job?> GetJob(JobId jobId)
    {
        var result = await connection.Services.Jobs.GetJob(new()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            JobId = jobId
        });

        return result.Value switch
        {
            Contracts.Jobs.Job job => job.ToClient(),
            Contracts.Jobs.JobError => null,
            _ => null
        };
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Job>> GetJobs()
    {
        var jobs = await connection.Services.Jobs.GetJobs(new()
        {
            EventStore = eventStoreName,
            Namespace = EventStoreNamespaceName.Default
        });
        return jobs.ToClient();
    }
}
