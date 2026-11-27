// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation.for_Observer.given;
using Cratis.Monads;
using Orleans.TestKit;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraints.given;

public class a_reindex_job : Specification
{
    const string EventStore = "event-store";
    const string Namespace = "namespace";

    protected readonly TestKitSilo _silo = new();
    protected readonly JobId _jobId = Guid.Parse("4f50ee31-9048-4da2-a066-ffb42771fc17");
    protected ReindexConstraints _job;
    protected provider_backed_storage<JobState> _state;

    protected async Task<JobSummary?> ReadJobById() =>
        (await JobSummary.AllJobs(EventStore, Namespace, _silo.GrainFactory)).SingleOrDefault(job => job.Id == (Guid)_jobId);

    async Task Establish()
    {
        var jobTypes = Substitute.For<IJobTypes>();
        jobTypes.GetFor(Arg.Any<Type>()).Returns(Result<JobType, IJobTypes.GetForError>.Success(new JobType("ReindexConstraints")));
        var storage = new InMemoryJobsStorage(jobTypes);
        _silo.AddService<IJobsStorage>(storage);
        _silo.AddService(jobTypes);

        var jobKey = new JobKey(EventStore, Namespace);
        var grainId = new GrainId(GrainType.Create("reindexconstraints"), GrainIdKeyExtensions.CreateGuidKey(_jobId, jobKey));
        _state = new(new JobGrainStorageProvider(storage), grainId) { State = new() };
        _silo.Options.StorageFactory = type => type == typeof(JobState) ? _state : null!;

        var jobs = Substitute.For<IJobsManager>();
        jobs.GetAllJobs().Returns(async _ => (await storage.GetFor(EventStore, Namespace).Jobs.GetJobs()).Match(result => result, error => throw error));
        _silo.AddProbe(_ => jobs);
        _job = await _silo.CreateGrainAsync<ReindexConstraints>(_jobId, jobKey);
    }
}
