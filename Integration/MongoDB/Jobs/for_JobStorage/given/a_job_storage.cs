// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Jobs;
using Cratis.Monads;
using MongoDB.Bson.Serialization;
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

        if (!BsonClassMap.IsClassMapRegistered(typeof(JobState)))
        {
            BsonClassMap.RegisterClassMap<JobState>(new JobStateClassMap().Configure);
        }

        var jobTypes = new TestJobTypes();
        var jobStateSerializer = new JobStateSerializer(jobTypes);
        BsonSerializer.RegisterSerializationProvider(
            new JobStateSerializationProvider(jobStateSerializer));

        _storage = new JobStorage(_database, jobTypes);

        await Task.CompletedTask;
    }

    async Task Cleanup()
    {
        await _mongoDatabase.Client.DropDatabaseAsync(_databaseName);
    }

    /// <summary>
    /// Minimal <see cref="IJobTypes"/> implementation for the <see cref="IntegrationJob"/> test job type.
    /// </summary>
    class TestJobTypes : IJobTypes
    {
        static readonly JobType _integrationJobType = new(nameof(IntegrationJob));

        public Result<JobType, IJobTypes.GetForError> GetFor(Type type)
        {
            if (type == typeof(IntegrationJob) || type.Name == nameof(IntegrationJob))
            {
                return _integrationJobType;
            }

            return IJobTypes.GetForError.NoAssociatedJobType;
        }

        public Result<Type, IJobTypes.GetClrTypeForError> GetClrTypeFor(JobType type)
        {
            if (type == _integrationJobType)
            {
                return typeof(IntegrationJob);
            }

            return IJobTypes.GetClrTypeForError.CouldNotFindType;
        }

        public Result<Type, IJobTypes.GetRequestClrTypeForError> GetRequestClrTypeFor(JobType type)
        {
            if (type == _integrationJobType)
            {
                return typeof(IntegrationJobRequest);
            }

            return IJobTypes.GetRequestClrTypeForError.CouldNotFindType;
        }
    }
}
