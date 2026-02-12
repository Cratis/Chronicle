// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Jobs;
using MongoDB.Driver;
using NSubstitute;

namespace Cratis.Chronicle.MongoDB.Integration.Jobs.for_JobStorage.given;

public class a_job_storage(ChronicleInProcessFixture fixture) : Integration.given.a_mongo_client(fixture)
{
    protected JobStorage _storage = default!;
    protected IEventStoreNamespaceDatabase _database = default!;
    protected IMongoDatabase _mongoDatabase = default!;
    protected string _databaseName = default!;

    async Task Establish()
    {
        _databaseName = $"chronicle_job_storage_specs_{Guid.NewGuid():N}";
        _mongoDatabase = _client.GetDatabase(_databaseName);

        _database = Substitute.For<IEventStoreNamespaceDatabase>();
        _database.GetCollection<JobState>(WellKnownCollectionNames.Jobs)
            .Returns(_mongoDatabase.GetCollection<JobState>(WellKnownCollectionNames.Jobs));

        // Use a simple IJobTypes that maps the test job type
        var jobTypes = CreateJobTypes();
        _storage = new JobStorage(_database, jobTypes);

        await Task.CompletedTask;
    }

    IJobTypes CreateJobTypes()
    {
        var jobTypes = Substitute.For<IJobTypes>();
        jobTypes.GetRequestClrTypeFor(Arg.Any<JobType>())
            .Returns(call =>
            {
                var jobType = call.Arg<JobType>();
                if (jobType.Value == "IntegrationJobRequest")
                {
                    return typeof(IntegrationJobRequest);
                }
                return IJobTypes.GetRequestClrTypeForError.CouldNotFindType;
            });
        return jobTypes;
    }

    async Task Cleanup()
    {
        await _mongoDatabase.Client.DropDatabaseAsync(_databaseName);
    }
}
