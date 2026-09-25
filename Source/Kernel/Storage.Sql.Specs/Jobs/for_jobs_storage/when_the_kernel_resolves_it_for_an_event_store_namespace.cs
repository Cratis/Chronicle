// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Cratis.Orleans.Storage.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using JobsDbContext = Cratis.Orleans.Storage.Sql.Jobs.JobsDbContext;

namespace Cratis.Chronicle.Storage.Sql.Jobs.for_jobs_storage;

/// <summary>
/// The kernel hands the job system nothing but <see cref="DbContextOptions"/>, built the way it builds every
/// other set - so whether the job tables exist is decided by that options shape, not by anything the kernel does
/// afterwards. Nothing in the kernel can provision them, and nothing complains when they are missing until a job
/// is written and the database says there is no such table.
/// </summary>
public class when_the_kernel_resolves_it_for_an_event_store_namespace : Specification, IDisposable
{
    string _databaseFile;
    Cratis.Orleans.Storage.JobsStorage _storage;
    Exception _error;
    IImmutableList<JobState> _jobs = [];

    void Establish() => _databaseFile = Path.Combine(Path.GetTempPath(), $"chronicle-jobs-{Guid.NewGuid():N}.db");

    async Task Because()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var jobTypes = Substitute.For<IJobTypes>();

        var sqlJobsStorage = new SqlJobsStorage(
            jobTypes,
            Options.Create(new SqlJobsStorageOptions { OptionsResolver = (_, _) => BuildOptionsTheWayTheKernelDoes() }),
            services);

        _error = await Catch.Exception(async () =>
        {
            _storage = sqlJobsStorage.GetFor("the-event-store", "the-namespace");

            var jobId = JobId.New();
            var save = await _storage.Jobs.Save(jobId, new JobState
            {
                Id = jobId,
                Details = new JobDetails("a job"),
                Type = new JobType("a-job-type"),
                Status = JobStatus.Running,
                Created = DateTimeOffset.UtcNow
            });
            save.RethrowError();

            var read = await _storage.Jobs.GetJobs();
            _jobs = read.Match(jobs => jobs, exception => throw exception);
        });
    }

    DbContextOptions<JobsDbContext> BuildOptionsTheWayTheKernelDoes()
    {
        // The same construction as Database.BuildOptions: the provider inferred from the connection string
        // through Arc, and concept support added on top.
        var builder = new DbContextOptionsBuilder<JobsDbContext>();
        builder.UseDatabaseFromConnectionString($"Data Source={_databaseFile}");
        builder.AddConceptAsSupport();
        return builder.Options;
    }

    [Fact] void should_have_the_job_tables() => _error.ShouldBeNull();

    [Fact] void should_read_back_the_job_it_wrote() => _jobs.Count.ShouldEqual(1);

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (File.Exists(_databaseFile))
        {
            File.Delete(_databaseFile);
        }
    }
}
