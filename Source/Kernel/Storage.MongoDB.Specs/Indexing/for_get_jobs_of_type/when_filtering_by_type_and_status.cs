// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Chronicle.Storage.MongoDB.Jobs;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Indexing.for_get_jobs_of_type;

/// <summary>
/// The typed job read is what tells the kernel whether a job for an observer is already running. It used to hand
/// <c>ToBsonDocument</c> on the filter definition to the driver, which serializes the definition object itself into
/// <c>{ _t: ... }</c> rather than rendering the filter, so the query matched no stored job and every caller was told
/// there were none.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class when_filtering_by_type_and_status(MongoDBFixture fixture) : given.a_real_namespace_database(fixture)
{
    static readonly JobType _jobType = new("the-job-type");
    static readonly JobType _otherJobType = new("another-job-type");

    List<JobId> _runningAndFailedOfType;
    List<JobId> _allOfType;

    async Task Because()
    {
        List<JobState> seeded =
        [
            Job(_jobType, JobStatus.Running),
            Job(_jobType, JobStatus.Stopped),
            Job(_jobType, JobStatus.Failed),
            Job(_otherJobType, JobStatus.Running)
        ];
        await _database.GetCollection<JobState>(WellKnownCollectionNames.Jobs).InsertManyAsync(seeded);

        var jobTypes = Substitute.For<IJobTypes>();
        jobTypes.GetFor(typeof(SampleJob)).Returns(_jobType);
        var storage = new JobStorage(_database, jobTypes);

        (await storage.GetJobs<SampleJob, JobState>(JobStatus.Running, JobStatus.Failed)).TryGetResult(out var narrowed);
        _runningAndFailedOfType = narrowed.Select(job => job.Id).ToList();

        (await storage.GetJobs<SampleJob, JobState>()).TryGetResult(out var all);
        _allOfType = all.Select(job => job.Id).ToList();
    }

    [Fact] void should_return_the_jobs_of_the_type_in_the_requested_statuses() => _runningAndFailedOfType.Count.ShouldEqual(2);
    [Fact] void should_return_every_job_of_the_type_when_no_status_is_given() => _allOfType.Count.ShouldEqual(3);

    static JobState Job(JobType jobType, JobStatus status) => new()
    {
        Id = JobId.New(),
        Type = jobType,
        Status = status,
        Created = DateTimeOffset.UtcNow,
        StatusChanges = [new JobStatusChanged { Status = status, Occurred = DateTimeOffset.UtcNow }]
    };
}
