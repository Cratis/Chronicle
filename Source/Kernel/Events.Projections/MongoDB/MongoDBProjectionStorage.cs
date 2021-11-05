// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Extensions.MongoDB;
using Cratis.Strings;
using Cratis.Events.Projections.Changes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Events.Projections.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionStorage"/> for working with projections in MongoDB.
    /// </summary>
    public class MongoDBProjectionStorage : IProjectionStorage
    {
        readonly IMongoDatabase _database;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDBProjectionStorage"/> class.
        /// </summary>
        /// <param name="clientFactory"><see cref="IMongoDBClientFactory"/>.</param>
        public MongoDBProjectionStorage(IMongoDBClientFactory clientFactory)
        {
            var client = clientFactory.Create(MongoUrl.Create("mongodb://localhost:27017"));
            _database = client.GetDatabase("read-models");
        }

        /// <inheritdoc/>
        public async Task<ExpandoObject> FindOrDefault(Model model, object key)
        {
            var collection = _database.GetCollection<BsonDocument>(model.Name);
            var result = await collection.FindAsync(Builders<BsonDocument>.Filter.Eq("_id", key.ToString()));
            var instance = result.SingleOrDefault();
            if (instance != default)
            {
                return BsonSerializer.Deserialize<ExpandoObject>(instance);
            }
            return new ExpandoObject();
        }

        /// <inheritdoc/>
        public async Task ApplyChanges(Model model, object key, Changeset changeset)
        {
            var updateDefinitionBuilder = Builders<BsonDocument>.Update;
            UpdateDefinition<BsonDocument>? updateBuilder = default;
            var hasChanges = false;

            foreach (var change in changeset.Changes)
            {
                if (change is PropertiesChanged propertiesChanged)
                {
                    foreach (var propertyDifference in propertiesChanged.Differences)
                    {
                        if (updateBuilder != default)
                        {
                            updateBuilder = updateBuilder.Set(propertyDifference.MemberPath.ToCamelCase(), propertyDifference.Changed);
                        }
                        else
                        {
                            updateBuilder = updateDefinitionBuilder.Set(propertyDifference.MemberPath.ToCamelCase(), propertyDifference.Changed);
                        }
                        hasChanges = true;
                    }
                }
            }

            if (!hasChanges) return;

            var collection = _database.GetCollection<BsonDocument>(model.Name);
            var filter = Builders<BsonDocument>.Filter.Eq("_id", key.ToString());
            await collection.UpdateOneAsync(filter, updateBuilder, new UpdateOptions { IsUpsert = true });

            await Task.CompletedTask;
        }
    }
}
