// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Changes;
using Cratis.Configuration;
using Cratis.Execution;
using Cratis.Extensions.MongoDB;
using Cratis.Strings;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Events.Projections.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionResultStore"/> for working with projections in MongoDB.
    /// </summary>
    public class MongoDBProjectionResultStore : IProjectionResultStore, IDisposable
    {
        readonly Model _model;
        readonly IExecutionContextManager _executionContextManager;
        readonly IMongoDBClientFactory _clientFactory;
        readonly Storage _configuration;
        IProjectionResultStoreRewindScope? _rewindScope;

        /// <summary>
        /// Gets the identifier of the <see cref="MongoDBProjectionResultStore"/>.
        /// </summary>
        public static readonly ProjectionResultStoreTypeId ProjectionResultStoreTypeId = "22202c41-2be1-4547-9c00-f0b1f797fd75";

        /// <inheritdoc/>
        public ProjectionResultStoreTypeName Name => "MongoDB";

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDBProjectionResultStore"/> class.
        /// </summary>
        /// <param name="model"><see cref="Model"/> the store is for.</param>
        /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
        /// <param name="clientFactory"><see cref="IMongoDBClientFactory"/>.</param>
        /// <param name="configuration"><see cref="Storage"/> configuration.</param>
        public MongoDBProjectionResultStore(
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

        /// <inheritdoc/>
        public ProjectionResultStoreTypeId TypeId => ProjectionResultStoreTypeId;

        /// <summary>
        /// Get the rewind collection name.
        /// </summary>
        /// <param name="name">Name to get for.</param>
        /// <returns>Formatted collection name for rewind purpose.</returns>
        public static string GetRewindCollectionName(string name) => $"rewind-{name}";

        /// <inheritdoc/>
        public async Task<ExpandoObject> FindOrDefault(object key)
        {
            var collection = GetCollection();
            var result = await collection.FindAsync(Builders<BsonDocument>.Filter.Eq("_id", key.ToString()));
            var instance = result.SingleOrDefault();
            if (instance != default)
            {
                return BsonSerializer.Deserialize<ExpandoObject>(instance);
            }
            return new ExpandoObject();
        }

        /// <inheritdoc/>
        public async Task ApplyChanges(object key, IChangeset<Event, ExpandoObject> changeset)
        {
            var updateDefinitionBuilder = Builders<BsonDocument>.Update;
            UpdateDefinition<BsonDocument>? updateBuilder = default;
            var hasChanges = false;

            var filter = Builders<BsonDocument>.Filter.Eq("_id", key.ToString());

            UpdateDefinition<BsonDocument> UpdateProperty(string path, object value)
            {
                if (updateBuilder != default)
                {
                    updateBuilder = updateBuilder.Set(path, value);
                }
                else
                {
                    updateBuilder = updateDefinitionBuilder!.Set(path, value);
                }

                return updateBuilder;
            }

            bool UpdateChangedProperties(IEnumerable<PropertyDifference<ExpandoObject>> differences, string? prefix = default)
            {
                foreach (var propertyDifference in differences)
                {
                    var propertyName = string.IsNullOrEmpty(prefix) ?
                        propertyDifference.MemberPath.ToCamelCase() :
                        $"{prefix}.{propertyDifference.MemberPath.ToCamelCase()}";

                    UpdateProperty(propertyName, propertyDifference.Changed!);
                }
                return differences.Any();
            }

            foreach (var change in changeset.Changes)
            {
                switch (change)
                {
                    case PropertiesChanged<ExpandoObject> propertiesChanged:
                        {
                            hasChanges = UpdateChangedProperties(propertiesChanged.Differences);
                        }
                        break;

                    case ChildAdded childAdded:
                        {
                            var document = childAdded.State.ToBsonDocument();
                            updateBuilder = updateDefinitionBuilder.AddToSet(childAdded.ChildrenProperty.Path, document);
                            hasChanges = true;
                        }
                        break;

                    case ChildPropertiesChanged<ExpandoObject> childPropertiesChanged:
                        {
                            var childValue = Builders<BsonDocument>.Filter.Eq(
                                $"{childPropertiesChanged.IdentifiedByProperty}", childPropertiesChanged.Key.ToString());
                            filter &= Builders<BsonDocument>.Filter.ElemMatch(childPropertiesChanged.ChildrenProperty.Path, childValue);

                            hasChanges = UpdateChangedProperties(childPropertiesChanged.Differences, $"{childPropertiesChanged.ChildrenProperty}.$");
                        }
                        break;
                }
            }

            if (!hasChanges) return;

            var collection = GetCollection();
            await collection.UpdateOneAsync(filter, updateBuilder, new UpdateOptions { IsUpsert = true });

            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task PrepareInitialRun()
        {
            var collection = GetCollection();
            return collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
        }

        /// <inheritdoc/>
        public IProjectionResultStoreRewindScope BeginRewind()
        {
            _rewindScope = new MongoDBProjectionResultStoreRewindScope(
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

        IMongoDatabase GetDatabase()
        {
            // TODO: Improve this! - Perhaps create a read model repository wrapper that caches per tenant.
            var eventStoreConfig = _configuration.Get(WellKnownStorageTypes.ReadModels);
            var tenantId = _executionContextManager.Current.TenantId.ToString()!;
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
}
