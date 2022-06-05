// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
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
    readonly Model _model;
    readonly IExecutionContextManager _executionContextManager;
    readonly IMongoDBClientFactory _clientFactory;
    readonly Storage _configuration;
    readonly IMongoDatabase _database;

    bool _isReplaying;

    /// <inheritdoc/>
    public ProjectionSinkTypeName Name => "MongoDB";

    /// <inheritdoc/>
    public ProjectionSinkTypeId TypeId => WellKnownProjectionSinkTypes.MongoDB;

    string ReplayCollectionName => $"replay-{_model.Name}";

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
        _database = GetDatabase();
    }

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
    public async Task BeginReplay()
    {
        _isReplaying = true;
        await PrepareInitialRun();
    }

    /// <inheritdoc/>
    public async Task EndReplay()
    {
        _isReplaying = false;
        var rewindName = ReplayCollectionName;
        var rewoundCollectionsPrefix = $"{_model.Name}-";
        var collectionNames = (await _database.ListCollectionNamesAsync()).ToList();
        var nextCollectionSequenceNumber = 1;
        var rewoundCollectionNames = collectionNames.Where(_ => _.StartsWith(rewoundCollectionsPrefix, StringComparison.InvariantCulture)).ToArray();
        if (rewoundCollectionNames.Length > 0)
        {
            nextCollectionSequenceNumber = rewoundCollectionNames
                .Select(_ =>
                {
                    var postfix = _.Substring(rewoundCollectionsPrefix.Length);
                    if (int.TryParse(postfix, out var value))
                    {
                        return value;
                    }
                    return -1;
                })
                .Where(_ => _ >= 0)
                .OrderByDescending(_ => _)
                .First() + 1;
        }
        var oldCollectionName = $"{rewoundCollectionsPrefix}{nextCollectionSequenceNumber}";

        if (collectionNames.Contains(_model.Name))
        {
            await _database.RenameCollectionAsync(_model.Name, oldCollectionName);
        }

        if (collectionNames.Contains(rewindName))
        {
            await _database.RenameCollectionAsync(rewindName, _model.Name);
        }
    }

    /// <inheritdoc/>
    public Task PrepareInitialRun()
    {
        var collection = GetCollection();
        return collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
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
                            propertyBuilder.AppendFormat("{0}.$[{1}]", segment.Value, collectionIdentifier);
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
        var tenantId = executionContext.TenantId.ToString()!;
        var readModelsConfig = _configuration.Microservices.Get(executionContext.MicroserviceId).Tenants[tenantId].Get(WellKnownStorageTypes.ReadModels);
        var url = new MongoUrl(readModelsConfig.ConnectionDetails.ToString());
        var client = _clientFactory.Create(url);
        return client.GetDatabase(url.DatabaseName);
    }

    IMongoCollection<BsonDocument> GetCollection() => _isReplaying ? _database.GetCollection<BsonDocument>(ReplayCollectionName) : _database.GetCollection<BsonDocument>(_model.Name);
}
