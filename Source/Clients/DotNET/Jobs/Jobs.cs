// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts;

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
        _servicesAccessor.Services.Jobs.StopJob(new()
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            JobId = jobId
        });

    /// <inheritdoc/>
    public Task Resume(JobId jobId) =>
        _servicesAccessor.Services.Jobs.ResumeJob(new()
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            JobId = jobId
        });

    /// <inheritdoc/>
    public Task Delete(JobId jobId) =>
        _servicesAccessor.Services.Jobs.DeleteJob(new()
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            JobId = jobId
        });

    /// <inheritdoc/>
    public async Task<Job?> GetJob(JobId jobId)
    {
        var jobs = await GetJobs();
        return jobs.FirstOrDefault(j => j.Id == jobId);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Job>> GetJobs()
    {
        var jobs = await _servicesAccessor.Services.Jobs.AllJobs(new()
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace
        }).FirstAsync();
        return (jobs ?? []).ToClient();
    }
}
