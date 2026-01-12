// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Jobs;
using Cratis.Monads;
using MongoDB.Driver;
using NSubstitute;

namespace Cratis.Chronicle.MongoDB.Integration.Jobs.for_JobStorage.given;

public class a_job_storage(ChronicleInProcessFixture fixture) : Specification(fixture)
{
    protected JobStorage _storage = default!;
    protected IJobTypes _jobTypes = default!;
    protected IEventStoreNamespaceDatabase _database = default!;
    protected IMongoDatabase _mongoDatabase = default!;
    protected string _databaseName = default!;

    async Task Establish()
    {
        var client = new MongoClient($"mongodb://localhost:{XUnit.Integration.ChronicleFixture.MongoDBPort}");
        _databaseName = $"chronicle_job_storage_specs_{Guid.NewGuid():N}";
        _mongoDatabase = client.GetDatabase(_databaseName);

        _database = Substitute.For<IEventStoreNamespaceDatabase>();
        _database.GetCollection<JobState>(WellKnownCollectionNames.Jobs)
            .Returns(_mongoDatabase.GetCollection<JobState>(WellKnownCollectionNames.Jobs));

        _jobTypes = Substitute.For<IJobTypes>();
        _jobTypes.GetRequestClrTypeFor(Arg.Is<JobType>(t => t.Value == "IntegrationJob"))
            .Returns(Result<Type, IJobTypes.GetRequestClrTypeForError>.Success(typeof(IntegrationJobRequest)));
        _jobTypes.GetFor(typeof(IntegrationJobRequest))
            .Returns(Result<JobType, IJobTypes.GetForError>.Success(new JobType("IntegrationJob")));

        _storage = new JobStorage(_database, _jobTypes);

        await Task.CompletedTask;
    }

    async Task Cleanup()
    {
        await _mongoDatabase.Client.DropDatabaseAsync(_databaseName);
    }
}
