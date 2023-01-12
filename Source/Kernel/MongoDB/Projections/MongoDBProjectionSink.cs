// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Text;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.MongoDB;
using Aksio.Cratis.Kernel.Configuration;
using Aksio.Cratis.Kernel.Engines.Projections;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Strings;
using MongoDB.Bson;
using MongoDB.Driver;
using NJsonSchema;
using IExpandoObjectConverter = Aksio.Cratis.Extensions.MongoDB.IExpandoObjectConverter;

namespace Aksio.Cratis.Events.Projections.MongoDB;

#pragma warning disable CA1849, MA0042 // MongoDB breaks the Orleans task model internally, so it won't return to the task scheduler

/// <summary>
/// Represents an implementation of <see cref="IProjectionSink"/> for working with projections in MongoDB.
/// </summary>
public class MongoDBProjectionSink : IProjectionSink, IDisposable
{
    readonly Model _model;
    readonly IExecutionContextManager _executionContextManager;
    readonly IMongoDBClientFactory _clientFactory;
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly ITypeFormats _typeFormats;
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
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
    /// <param name="typeFormats">The <see cref="ITypeFormats"/> for looking up actual types.</param>
    /// <param name="configuration"><see cref="Storage"/> configuration.</param>
    public MongoDBProjectionSink(
        Model model,
        IExecutionContextManager executionContextManager,
        IMongoDBClientFactory clientFactory,
        IExpandoObjectConverter expandoObjectConverter,
        ITypeFormats typeFormats,
        Storage configuration)
    {
        _model = model;
        _executionContextManager = executionContextManager;
        _clientFactory = clientFactory;
        _expandoObjectConverter = expandoObjectConverter;
        _typeFormats = typeFormats;
        _configuration = configuration;
        _database = GetDatabase();
    }

    /// <inheritdoc/>
    public async Task<ExpandoObject?> FindOrDefault(Key key)
    {
        var collection = GetCollection();

        var result = await collection.FindAsync(Builders<BsonDocument>.Filter.Eq("_id", ConvertKeyToBsonValue(key)));
        var instance = result.SingleOrDefault();
        if (instance != default)
        {
            return _expandoObjectConverter.ToExpandoObject(instance, _model.Schema);
        }

        return default;
    }

    /// <inheritdoc/>
    public async Task ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset)
    {
        var updateDefinitionBuilder = Builders<BsonDocument>.Update;
        UpdateDefinition<BsonDocument>? updateBuilder = default;
        var hasChanges = false;

        var filter = Builders<BsonDocument>.Filter.Eq("_id", ConvertKeyToBsonValue(key));
        var collection = GetCollection();

        if (changeset.HasBeenRemoved())
        {
            await collection.DeleteOneAsync(filter);
            return;
        }

        var arrayFiltersForDocument = new List<BsonDocumentArrayFilterDefinition<BsonDocument>>();
        await ApplyActualChanges(key, changeset.Changes, updateDefinitionBuilder, ref updateBuilder, ref hasChanges, arrayFiltersForDocument);
        var distinctArrayFilters = arrayFiltersForDocument.DistinctBy(_ => _.Document).ToArray();

        if (!hasChanges) return;

        // var rendered = updateBuilder!.Render(BsonSerializer.LookupSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry);
        await collection.UpdateOneAsync(
            filter,
            updateBuilder,
            new UpdateOptions
            {
                IsUpsert = true,
                ArrayFilters = distinctArrayFilters
            });
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

    Task ApplyActualChanges(
        Key key,
        IEnumerable<Change> changes,
        UpdateDefinitionBuilder<BsonDocument> updateDefinitionBuilder,
        ref UpdateDefinition<BsonDocument>? updateBuilder,
        ref bool hasChanges,
        List<BsonDocumentArrayFilterDefinition<BsonDocument>> arrayFiltersForDocument)
    {
        var joinTasks = new List<Task>();

        foreach (var change in changes)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject> propertiesChanged:
                    {
                        var allArrayFilters = new List<BsonDocumentArrayFilterDefinition<BsonDocument>>();

                        foreach (var propertyDifference in propertiesChanged.Differences)
                        {
                            var (property, arrayFilters) = ConvertToMongoDBProperty(propertyDifference.PropertyPath, key.ArrayIndexers);
                            allArrayFilters.AddRange(arrayFilters);

                            var value = ConvertToBsonValueForKnownTargetProperty(propertyDifference.Changed, propertyDifference.PropertyPath);

                            if (updateBuilder != default)
                            {
                                updateBuilder = updateBuilder.Set(property, value);
                            }
                            else
                            {
                                updateBuilder = updateDefinitionBuilder.Set(property, value);
                            }
                        }

                        arrayFiltersForDocument.AddRange(allArrayFilters);

                        hasChanges = propertiesChanged.Differences.Any();
                    }
                    break;

                case ChildAdded childAdded:
                    {
                        var schema = _model.Schema.GetSchemaForPropertyPath(childAdded.ChildrenProperty);
                        var document = _expandoObjectConverter.ToBsonDocument((childAdded.State as ExpandoObject)!, schema);

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

                        if (updateBuilder is not null)
                        {
                            updateBuilder = updateBuilder.Push(property, document);
                        }
                        else
                        {
                            updateBuilder = updateDefinitionBuilder.Push(property, document);
                        }

                        hasChanges = true;
                    }
                    break;

                case Joined joined:
                    {
                        var (property, arrayFilters) = ConvertToMongoDBProperty(joined.OnProperty, joined.ArrayIndexers);

                        UpdateDefinition<BsonDocument>? joinUpdateBuilder = default;
                        var hasJoinChanges = false;

                        var serializedKey = GetBsonValueFrom(key.Value);
                        var filter = Builders<BsonDocument>.Filter.Eq(property, joined.Key);

                        var collection = GetCollection();

                        var joinArrayFiltersForDocument = new List<BsonDocumentArrayFilterDefinition<BsonDocument>>();
                        ApplyActualChanges(key, joined.Changes, updateDefinitionBuilder, ref joinUpdateBuilder, ref hasJoinChanges, joinArrayFiltersForDocument).Wait();

                        if (hasJoinChanges)
                        {
                            // var rendered = joinUpdateBuilder!.Render(BsonSerializer.LookupSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry);
                            joinTasks.Add(collection.UpdateOneAsync(
                                filter,
                                joinUpdateBuilder,
                                new UpdateOptions
                                {
                                    IsUpsert = false,
                                    ArrayFilters = joinArrayFiltersForDocument.ToArray()
                                }));
                        }
                    }
                    break;

                case ResolvedJoin resolvedJoined:
                    {
                        ApplyActualChanges(key, resolvedJoined.Changes, updateDefinitionBuilder, ref updateBuilder, ref hasChanges, arrayFiltersForDocument);
                    }
                    break;
            }
        }

        return Task.WhenAll(joinTasks);
    }

    BsonValue ConvertKeyToBsonValue(Key key)
    {
        var bsonValue = key.Value is ExpandoObject ?
                _expandoObjectConverter.ToBsonDocument((key.Value as ExpandoObject)!, _model.Schema.GetSchemaForPropertyPath("id")) :
                ConvertToBsonValueForKnownTargetProperty(key.Value, "id");

        // If the schema does not have the Id property, we assume it is the event source identifier, which is of type string.
        if (bsonValue == BsonNull.Value)
        {
            return new BsonString(key.Value.ToString());
        }

        return bsonValue;
    }

    BsonValue ConvertToBsonValueForKnownTargetProperty(object? input, PropertyPath property)
    {
        BsonValue value = BsonNull.Value;
        var schemaProperty = _model.Schema.GetSchemaPropertyForPropertyPath(property);
        if (schemaProperty is not null)
        {
            return ConvertToBsonValue(input, schemaProperty);
        }

        return value;
    }

    BsonValue ConvertToBsonValue(object? input, JsonSchemaProperty schemaProperty)
    {
        if (_typeFormats.IsKnown(schemaProperty.Format))
        {
            var targetType = _typeFormats.GetTypeForFormat(schemaProperty.Format);
            return input.ToBsonValue(targetType);
        }

        var value = input.ToBsonValueBasedOnSchemaPropertyType(schemaProperty);
        if (value == BsonNull.Value && input is ExpandoObject expandoObject)
        {
            return _expandoObjectConverter.ToBsonDocument(expandoObject, schemaProperty.ActualTypeSchema);
        }

        if (value == BsonNull.Value && value is IEnumerable enumerable)
        {
            var items = new List<BsonValue>();
            foreach (var item in enumerable)
            {
                items.Add(ConvertToBsonValue(item, schemaProperty));
            }
            return new BsonArray(items);
        }

        return value;
    }

    BsonValue GetBsonValueFrom(object value)
    {
        if (value is ExpandoObject expandoObject)
        {
            var expandoObjectAsDictionary = expandoObject as IDictionary<string, object>;
            var document = new BsonDocument();

            foreach (var kvp in expandoObjectAsDictionary)
            {
                document[GetNameForPropertyInBsonDocument(kvp.Key)] = GetBsonValueFrom(kvp.Value);
            }

            return document;
        }

        var bsonValue = value.ToBsonValue();
        if (bsonValue == BsonNull.Value && value is IEnumerable enumerable)
        {
            var array = new BsonArray();

            foreach (var item in enumerable)
            {
                array.Add(GetBsonValueFrom(item));
            }

            return array;
        }

        return bsonValue;
    }

    (string Property, IEnumerable<BsonDocumentArrayFilterDefinition<BsonDocument>> ArrayFilters) ConvertToMongoDBProperty(PropertyPath propertyPath, IArrayIndexers arrayIndexers)
    {
        var arrayFilters = new List<BsonDocumentArrayFilterDefinition<BsonDocument>>();
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
                            var filter = new ExpandoObject();
                            ((IDictionary<string, object?>)filter).Add($"{collectionIdentifier}.{arrayIndexer.IdentifierProperty}", arrayIndexer.Identifier);
                            var document = GetBsonValueFrom(filter) as BsonDocument;
                            arrayFilters.Add(new BsonDocumentArrayFilterDefinition<BsonDocument>(document));
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

    string GetNameForPropertyInBsonDocument(string name)
    {
        if (name == "id")
        {
            return "_id";
        }
        return name;
    }
}
