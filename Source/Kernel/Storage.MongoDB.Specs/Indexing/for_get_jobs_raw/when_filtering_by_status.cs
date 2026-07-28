// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Chronicle.Storage.MongoDB.Jobs;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Indexing.for_get_jobs_raw;

[Collection(MongoDBCollection.Name)]
public class when_filtering_by_status(MongoDBFixture fixture) : given.a_real_namespace_database(fixture)
{
    static readonly JobStatus[] _requested = [JobStatus.Running, JobStatus.Failed];

    List<JobState> _seeded;
    List<JobId> _newResult;
    List<JobId> _expectedByLastStatusChange;
    List<JobId> _allJobs;
    IReadOnlyList<string> _indexes;

    async Task Because()
    {
        _seeded =
        [
            Job(JobStatus.Running),
            Job(JobStatus.Stopped),
            Job(JobStatus.CompletedSuccessfully),
            Job(JobStatus.Running),
            Job(JobStatus.Failed)
        ];
        await _database.GetCollection<JobState>(WellKnownCollectionNames.Jobs).InsertManyAsync(_seeded);

        var storage = new JobStorage(_database, Substitute.For<IJobTypes>());

        (await storage.GetJobs(_requested)).TryGetResult(out var filtered);
        _newResult = filtered.Select(job => job.Id).ToList();

        // The previous implementation matched on the last element of statusChanges; replicate that semantics
        // in-memory so the rewrite is proven to return the same set.
        _expectedByLastStatusChange = _seeded
            .Where(job => _requested.Contains(job.StatusChanges[^1].Status))
            .Select(job => job.Id)
            .ToList();

        (await storage.GetJobs()).TryGetResult(out var all);
        _allJobs = all.Select(job => job.Id).ToList();

        _indexes = await IndexNamesFor(WellKnownCollectionNames.Jobs);
    }

    [Fact] void should_return_the_same_set_as_the_previous_status_change_filter() => _newResult.ShouldContainOnly(_expectedByLastStatusChange);
    [Fact] void should_return_the_running_and_failed_jobs_only() => _newResult.Count.ShouldEqual(3);
    [Fact] void should_return_all_jobs_when_no_status_is_given() => _allJobs.Count.ShouldEqual(_seeded.Count);
    [Fact] void should_create_the_job_status_index() => _indexes.ShouldContain("status");
    [Fact] void should_create_the_job_type_and_status_index() => _indexes.ShouldContain("type_status");

    static JobState Job(JobStatus status) => new()
    {
        Id = JobId.New(),
        Status = status,
        Created = DateTimeOffset.UtcNow,
        StatusChanges =
        [
            new JobStatusChanged { Status = JobStatus.PreparingJob, Occurred = DateTimeOffset.UtcNow },
            new JobStatusChanged { Status = status, Occurred = DateTimeOffset.UtcNow }
        ]
    };
}
