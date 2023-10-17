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

    async Task<List<BsonDocument>> GetJobsRaw(params JobStatus[] statuses)
    {
        var statusFilters = statuses.Select(status => Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, JobStatus>(nameof(JobState<object>.Status).ToCamelCase()), status));
        var filter = statuses.Length == 0 ?
                                Builders<BsonDocument>.Filter.Empty :
                                Builders<BsonDocument>.Filter.Or(statusFilters);

        var cursor = await Collection.FindAsync(filter).ConfigureAwait(false);
        return await cursor.ToListAsync().ConfigureAwait(false);
    }

    void HandleChangesForJobs(IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> cursor, List<JobState<object>> observers)
    {
        foreach (var changedJob in cursor.Current.Select(_ => _.FullDocument))
        {
            var jobTypeString = changedJob["type"].AsString;
            var jobType = Type.GetType(jobTypeString);
            if (jobType is not null)
            {
                var interfaces = jobType.GetInterfaces();
                var observer = observers.Find(_ => _.Id == (JobId)changedJob["_id"].AsGuid);

                var jobState = BsonSerializer.Deserialize<JobState<object>>(changedJob);
                if (observer is not null)
                {
                    var index = observers.IndexOf(observer);
                    observers[index] = jobState;
                }
                else
                {
                    observers.Add(jobState);
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
    public Task Remove(JobId jobId)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Save(JobId jobId, TJobState state)
    {
        var filter = GetIdFilter(jobId);
        var document = state.ToBsonDocument();
        document.Remove("_id");
        RemoveTypeInfo(document);
        await Collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
    }

    bool RemoveTypeInfo(BsonDocument document, BsonDocument? parent = null, string? childProperty = null)
    {
        var elementsToRemove = new List<string>();

        foreach (var child in document.Where(_ => _.Value is BsonDocument))
        {
            if (RemoveTypeInfo(child.Value.AsBsonDocument, document, child.Name))
            {
                elementsToRemove.Add(child.Name);
            }
        }

        foreach (var elementToRemove in elementsToRemove)
        {
            document.Remove(elementToRemove);
        }

        if (!string.IsNullOrEmpty(childProperty) && parent is not null)
        {
            return document.Any(_ => _.Name == "_t");
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<TJobState>> GetJobs<TJobType>(params JobStatus[] statuses)
    {
        var jobType = (JobType)typeof(TJobType);
        var jobTypeFilter = Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, JobType>(nameof(JobState<object>.Type).ToCamelCase()), jobType);
        var statusFilters = statuses.Select(status => Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, JobStatus>(nameof(JobState<object>.Status).ToCamelCase()), status));

        var filter = statuses.Length == 0 ?
                                jobTypeFilter :
                                Builders<BsonDocument>.Filter.And(jobTypeFilter, Builders<BsonDocument>.Filter.Or(statusFilters));

        var cursor = await Collection.FindAsync(filter).ConfigureAwait(false);
        var documents = await cursor.ToListAsync().ConfigureAwait(false);
        return documents.Select(_ => BsonSerializer.Deserialize<TJobState>(_)).ToImmutableList();
    }

    FilterDefinition<BsonDocument> GetIdFilter(Guid id) => Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, Guid>("_id"), id);
}
