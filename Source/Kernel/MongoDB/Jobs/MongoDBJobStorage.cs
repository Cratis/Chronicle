// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.MongoDB.Observation;
using Aksio.Cratis.Kernel.Persistence.Jobs;
using Aksio.DependencyInversion;
using Aksio.Strings;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobStorage"/> for MongoDB.
/// </summary>
public class MongoDBJobStorage : IJobStorage
{
    readonly ProviderFor<IEventStoreDatabase> _databaseProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBJobStorage{TJobState}"/> class.
    /// </summary>
    /// <param name="databaseProvider">Provider for <see cref="IEventStoreDatabase"/> for persistence.</param>
    public MongoDBJobStorage(ProviderFor<IEventStoreDatabase> databaseProvider)
    {
        _databaseProvider = databaseProvider;
    }

    /// <summary>
    /// Gets the <see cref="IMongoCollection{TDocument}"/> to work with.
    /// </summary>
    protected IMongoCollection<BsonDocument> Collection => _databaseProvider().GetCollection<BsonDocument>(WellKnownCollectionNames.Jobs);

    /// <inheritdoc/>
    public async Task<JobState<object>> GetJob(JobId jobId)
    {
        var filter = GetIdFilter(jobId);
        var cursor = await Collection.FindAsync(filter).ConfigureAwait(false);
        var job = cursor.Single();
        return BsonSerializer.Deserialize<JobState<object>>(job);
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<JobState<object>>> GetJobs(params JobStatus[] statuses)
    {
        var documents = await GetJobsRaw(statuses).ConfigureAwait(false);
        return documents.Select(_ => BsonSerializer.Deserialize<JobState<object>>(_)).ToImmutableList();
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<JobState<object>>> ObserveJobs(params JobStatus[] statuses)
    {
        var statusFilters = statuses.Select(status =>
            Builders<ChangeStreamDocument<BsonDocument>>.Filter.Eq(
                new StringFieldDefinition<ChangeStreamDocument<BsonDocument>, JobStatus>($"fullDocument.{nameof(JobState<object>.Status).ToCamelCase()}"), status));

        var initialItems = GetJobs(statuses).GetAwaiter().GetResult();

        var filter = statuses.Length == 0 ?
                                Builders<ChangeStreamDocument<BsonDocument>>.Filter.Empty :
                                Builders<ChangeStreamDocument<BsonDocument>>.Filter.Or(statusFilters);

        return Collection.Observe(initialItems, HandleChangesForJobs, filter);
    }

    /// <inheritdoc/>
    public async Task Remove(JobId jobId) =>
        await Collection.DeleteOneAsync(GetIdFilter(jobId)).ConfigureAwait(false);

    /// <summary>
    /// Get the filter for a given <see cref="JobId"/>.
    /// </summary>
    /// <param name="id"><see cref="Guid"/> identifier of the job.</param>
    /// <returns><see cref="FilterDefinition{T}"/> for the BsonDocument.</returns>
    protected FilterDefinition<BsonDocument> GetIdFilter(Guid id) => Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, Guid>("_id"), id);

    async Task<List<BsonDocument>> GetJobsRaw(params JobStatus[] statuses)
    {
        var statusFilters = statuses.Select(status => Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, JobStatus>(nameof(JobState<object>.Status).ToCamelCase()), status));

        if (statuses.Length == 0)
        {
            var cursor = await Collection.FindAsync(Builders<BsonDocument>.Filter.Empty).ConfigureAwait(false);
            return await cursor.ToListAsync().ConfigureAwait(false);
        }

        var filter = new BsonDocument
        {
            {
                "$expr", new BsonDocument("$in", new BsonArray
                {
                    new BsonDocument(
                        "$arrayElemAt",
                        new BsonArray
                        {
                                "$statusChanges.status",
                                -1
                        }),
                    new BsonArray(statuses)
                })
            }
        };

        var aggregation = Collection.Aggregate().Match(filter);
        return await aggregation.ToListAsync().ConfigureAwait(false);
    }

    void HandleChangesForJobs(IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> cursor, List<JobState<object>> jobs)
    {
        foreach (var changedJob in cursor.Current.Select(_ => _.FullDocument))
        {
            var jobTypeString = changedJob["type"].AsString;
            var jobType = Type.GetType(jobTypeString);
            if (jobType is not null)
            {
                var interfaces = jobType.GetInterfaces();
                var observer = jobs.Find(_ => _.Id == (JobId)changedJob["_id"].AsGuid);

                var jobState = BsonSerializer.Deserialize<JobState<object>>(changedJob);
                if (observer is not null)
                {
                    var index = jobs.IndexOf(observer);
                    jobs[index] = jobState;
                }
                else
                {
                    jobs.Add(jobState);
                }
            }
        }
    }
}

/// <summary>
/// Represents an implementation of <see cref="IJobStorage{TJobState}"/> for MongoDB.
/// </summary>
/// <typeparam name="TJobState">Type of <see cref="JobState{T}"/> to work with.</typeparam>
public class MongoDBJobStorage<TJobState> : MongoDBJobStorage, IJobStorage<TJobState>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBJobStorage{TJobState}"/> class.
    /// </summary>
    /// <param name="databaseProvider">Provider for <see cref="IEventStoreDatabase"/> for persistence.</param>
    public MongoDBJobStorage(ProviderFor<IEventStoreDatabase> databaseProvider) : base(databaseProvider)
    {
    }

    /// <inheritdoc/>
    public async Task<TJobState?> Read(JobId jobId)
    {
        var filter = GetIdFilter(jobId);
        var cursor = await Collection.FindAsync(filter).ConfigureAwait(false);
        var state = cursor.FirstOrDefault();
        if (state is not null)
        {
            return BsonSerializer.Deserialize<TJobState>(state);
        }

        return default!;
    }

    /// <inheritdoc/>
    public async Task Save(JobId jobId, TJobState state)
    {
        var filter = GetIdFilter(jobId);
        var document = state.ToBsonDocument();
        document.Remove("_id");
        document.RemoveTypeInfo();
        await Collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<TJobState>> GetJobs<TJobType>(params JobStatus[] statuses)
    {
        var jobType = (JobType)typeof(TJobType);
        var jobTypeFilter = Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, string>(nameof(JobState<object>.Type).ToCamelCase()), jobType);
        var statusFilters = statuses.Select(status => Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, JobStatus>(nameof(JobState<object>.Status).ToCamelCase()), status));

        var filter = statuses.Length == 0 ?
                                jobTypeFilter :
                                Builders<BsonDocument>.Filter.And(jobTypeFilter, Builders<BsonDocument>.Filter.Or(statusFilters));

        var cursor = await Collection.FindAsync(filter).ConfigureAwait(false);
        var documents = await cursor.ToListAsync().ConfigureAwait(false);
        return documents.Select(_ => BsonSerializer.Deserialize<TJobState>(_)).ToImmutableList();
    }
}
