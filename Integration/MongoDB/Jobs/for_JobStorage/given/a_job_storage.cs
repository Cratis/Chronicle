// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Jobs;
using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        var jobTypes = Services.GetRequiredService<IJobTypes>();
        _storage = new JobStorage(_database, jobTypes);

        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override void ConfigureServices(IServiceCollection services)
    {
        // Remove convention-based IJobTypes registration so we can replace it
        // with a decorator that adds test-specific job type mappings.
        // This ensures the JobStateSerializer (which resolves IJobTypes from DI)
        // can deserialize test request types.
        services.RemoveAll<IJobTypes>();
        services.AddSingleton<IJobTypes>(sp =>
        {
            var types = sp.GetRequiredService<ITypes>();
            var realJobTypes = new JobTypes(types);
            return new TestJobTypes(realJobTypes);
        });
    }

    async Task Cleanup()
    {
        await _mongoDatabase.Client.DropDatabaseAsync(_databaseName);
    }

    /// <summary>
    /// Decorator over <see cref="IJobTypes"/> that adds test-specific job type to request type mappings
    /// while delegating all other calls to the real implementation.
    /// </summary>
    /// <param name="inner">The real <see cref="IJobTypes"/> implementation.</param>
    class TestJobTypes(IJobTypes inner) : IJobTypes
    {
        public Result<JobType, IJobTypes.GetForError> GetFor(Type type) => inner.GetFor(type);

        public Result<Type, IJobTypes.GetClrTypeForError> GetClrTypeFor(JobType type) => inner.GetClrTypeFor(type);

        public Result<Type, IJobTypes.GetRequestClrTypeForError> GetRequestClrTypeFor(JobType type)
        {
            if (type.Value == nameof(IntegrationJob))
            {
                return typeof(IntegrationJobRequest);
            }

            return inner.GetRequestClrTypeFor(type);
        }
    }
}
