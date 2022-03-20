// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using System.Text;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Configuration;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.MongoDB;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Strings;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Aksio.Cratis.Events.Projections.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IProjectionSink"/> for working with projections in MongoDB.
/// </summary>
public class MongoDBProjectionSink : IProjectionSink, IDisposable
{
    /// <summary>
    /// Gets the identifier of the <see cref="MongoDBProjectionSink"/>.
    /// </summary>
    public static readonly ProjectionSinkTypeId ProjectionResultStoreTypeId = "22202c41-2be1-4547-9c00-f0b1f797fd75";

    readonly Model _model;
    readonly IExecutionContextManager _executionContextManager;
    readonly IMongoDBClientFactory _clientFactory;
    readonly Storage _configuration;
    IProjectionSinkRewindScope? _rewindScope;

    /// <inheritdoc/>
    public ProjectionSinkTypeName Name => "MongoDB";

    /// <inheritdoc/>
    public ProjectionSinkTypeId TypeId => ProjectionResultStoreTypeId;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBProjectionSink"/> class.
    /// </summary>
    /// <param name="model"><see cref="Model"/> the store is for.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
    /// <param name="clientFactory"><see cref="IMongoDBClientFactory"/>.</param>
    /// <param name="configuration"><see cref="Storage"/> configuration.</param>
    public MongoDBProjectionSink(
        Model model,
        IExecutionContextManager executionContextManager,
        IMongoDBClientFactory clientFactory,
        Storage configuration)
    {
        _model = model;
        _executionContextManager = executionContextManager;
        _clientFactory = clientFactory;
        _configuration = configuration;
    }

    /// <summary>
    /// Get the rewind collection name.
    /// </summary>
    /// <param name="name">Name to get for.</param>
    /// <returns>Formatted collection name for rewind purpose.</returns>
    public static string GetRewindCollectionName(string name) => $"rewind-{name}";

    /// <inheritdoc/>
    public async Task<ExpandoObject> FindOrDefault(Key key)
    {
        var collection = GetCollection();
        var result = await collection.FindAsync(Builders<BsonDocument>.Filter.Eq("_id", key.Value.ToString()));
        var instance = result.SingleOrDefault();
        if (instance != default)
        {
            return BsonSerializer.Deserialize<ExpandoObject>(instance);
        }
        return new ExpandoObject();
    }

    /// <inheritdoc/>
    public async Task ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset)
    {
        var updateDefinitionBuilder = Builders<BsonDocument>.Update;
        UpdateDefinition<BsonDocument>? updateBuilder = default;
        var hasChanges = false;

        var filter = Builders<BsonDocument>.Filter.Eq("_id", key.Value.ToString());
        var collection = GetCollection();

        if (changeset.HasBeenRemoved())
        {
            await collection.DeleteOneAsync(filter);
            return;
        }

        var arrayFiltersForDocument = new List<ArrayFilterDefinition>();

        foreach (var change in changeset.Changes)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject> propertiesChanged:
                    {
                        var allArrayFilters = new List<ArrayFilterDefinition>();

                        foreach (var propertyDifference in propertiesChanged.Differences)
                        {
                            var (property, arrayFilters) = ConvertToMongoDBProperty(propertyDifference.PropertyPath, key.ArrayIndexers);
                            allArrayFilters.AddRange(arrayFilters);

                            if (updateBuilder != default)
                            {
                                updateBuilder = updateBuilder.Set(property, propertyDifference.Changed!);
                            }
                            else
                            {
                                updateBuilder = updateDefinitionBuilder!.Set(property, propertyDifference.Changed!);
                            }
                        }

                        arrayFiltersForDocument.AddRange(allArrayFilters);

                        hasChanges = propertiesChanged.Differences.Any();
                    }
                    break;

                case ChildAdded childAdded:
                    {
                        var document = childAdded.State.ToBsonDocument();

                        var segments = childAdded.ChildrenProperty.Segments.ToArray();
                        var childrenProperty = new PropertyPath(string.Empty);
                        for (var i = 0; i < segments.Length - 1; i++)
                        {
                            childrenProperty += segments[i].ToString()!;
                        }

                        childrenProperty += segments[^1].Value;
                        var arrayIndexers = new ArrayIndexers(key.ArrayIndexers.All.Where(_ => !_.ArrayProperty.Equals(childAdded.ChildrenProperty)));
                        var (property, arrayFilters) = ConvertToMongoDBProperty(childrenProperty, arrayIndexers);
                        arrayFiltersForDocument.AddRange(arrayFilters);
                        updateBuilder = updateDefinitionBuilder.AddToSet(property, document);
                        hasChanges = true;
                    }
                    break;
            }
        }

        if (!hasChanges) return;

        await collection.UpdateOneAsync(
            filter,
            updateBuilder,
            new UpdateOptions
            {
                IsUpsert = true,
                ArrayFilters = arrayFiltersForDocument.ToArray()
            });

        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task PrepareInitialRun()
    {
        var collection = GetCollection();
        return collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
    }

    /// <inheritdoc/>
    public IProjectionSinkRewindScope BeginRewind()
    {
        _rewindScope = new MongoDBProjectionSinkRewindScope(
            GetDatabase(),
            _model,
            () => _rewindScope = default);
        return _rewindScope;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _rewindScope?.Dispose();
        GC.SuppressFinalize(this);
    }

    (string Property, IEnumerable<ArrayFilterDefinition> ArrayFilters) ConvertToMongoDBProperty(PropertyPath propertyPath, IArrayIndexers arrayIndexers)
    {
        var arrayFilters = new List<ArrayFilterDefinition>();
        var propertyBuilder = new StringBuilder();
        var currentPropertyPath = new PropertyPath(string.Empty);

        foreach (var segment in propertyPath.Segments)
        {
            if (propertyBuilder.Length > 0)
            {
                propertyBuilder.Append('.');
            }

            currentPropertyPath += segment;
            switch (segment)
            {
                case PropertyName:
                    {
                        propertyBuilder.Append(segment.Value);
                    }
                    break;

                case ArrayProperty:
                    {
                        var collectionIdentifier = currentPropertyPath.LastSegment.Value.ToCamelCase();
                        var arrayIndexer = arrayIndexers.GetFor(currentPropertyPath);
                        if (arrayIndexer is not null)
                        {
                            propertyBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0}.$[{1}]", segment.Value, collectionIdentifier);
                            arrayFilters.Add(new BsonDocumentArrayFilterDefinition<BsonDocument>(
                                new BsonDocument(
                                    new Dictionary<string, object>
                                    {
                                            { $"{collectionIdentifier}.{arrayIndexer.IdentifierProperty}", arrayIndexer.Identifier }
                                    })));
                        }
                        else
                        {
                            propertyBuilder.Append(segment.Value);
                        }
                    }
                    break;
            }
        }

        return (Property: propertyBuilder.ToString(), ArrayFilters: arrayFilters.ToArray());
    }

    IMongoDatabase GetDatabase()
    {
        // TODO: Improve this! - Perhaps create a read model repository wrapper that caches per tenant.
        var executionContext = _executionContextManager.Current;
        var eventStoreConfig = _configuration.Microservices[executionContext.MicroserviceId].Get(WellKnownStorageTypes.ReadModels);
        var tenantId = executionContext.TenantId.ToString()!;
        var url = new MongoUrl(eventStoreConfig.Tenants[tenantId].ToString());
        var client = _clientFactory.Create(url);
        return client.GetDatabase(url.DatabaseName);
    }

    IMongoCollection<BsonDocument> GetCollection()
    {
        var database = GetDatabase();
        return IsRewinding ? database.GetCollection<BsonDocument>(GetRewindCollectionName(_model.Name)) : database.GetCollection<BsonDocument>(_model.Name);
    }

    bool IsRewinding => _rewindScope != default;
}
