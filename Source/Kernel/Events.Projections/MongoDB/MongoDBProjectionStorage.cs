// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Events.Projections.Changes;
using Cratis.Extensions.MongoDB;
using Cratis.Strings;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

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
            var settings = MongoClientSettings.FromConnectionString("mongodb://localhost:27017");
            settings.ClusterConfigurator = _ => _
                .Subscribe<CommandStartedEvent>(ev => Console.WriteLine($"Start: {ev.CommandName} : {ev.Command}"))
                .Subscribe<CommandSucceededEvent>(ev => Console.WriteLine($"Succeeded: {ev.CommandName} : {ev.Reply}"))
                .Subscribe<CommandFailedEvent>(ev => Console.WriteLine($"Failed: {ev.CommandName} : {ev.Failure}"));

            var client = clientFactory.Create(settings.Freeze());
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

            foreach (var change in changeset.Changes)
            {
                switch (change)
                {
                    case PropertiesChanged propertiesChanged:
                        {
                            foreach (var propertyDifference in propertiesChanged.Differences)
                            {
                                UpdateProperty(propertyDifference.MemberPath.ToCamelCase(), propertyDifference.Changed!);
                                hasChanges = true;
                            }
                        }
                        break;
                    case ChildAdded childAdded:
                        {
                        }
                        break;

                    case ChildPropertiesChanged childPropertiesChanged:
                        {
                            if (changeset.Changes
                                            .Select(_ => _ as ChildAdded)
                                            .Any(_ => _ != null && _.ChildrenProperty == childPropertiesChanged.ChildrenProperty && _.Key == childPropertiesChanged.Key))
                            {
                                var document = childPropertiesChanged.State.ToBsonDocument();
                                updateBuilder = updateDefinitionBuilder.AddToSet(childPropertiesChanged.ChildrenProperty.Path, document);
                                hasChanges = true;
                            }
                            else
                            {
                                var childValue = Builders<BsonDocument>.Filter.Eq(
                                    $"{childPropertiesChanged.IdentifiedByProperty}", childPropertiesChanged.Key.ToString());
                                filter &= Builders<BsonDocument>.Filter.ElemMatch(childPropertiesChanged.ChildrenProperty.Path, childValue);

                                var prefix = $"{childPropertiesChanged.ChildrenProperty}.$";
                                foreach (var propertyDifference in childPropertiesChanged!.Differences)
                                {
                                    UpdateProperty($"{prefix}.{propertyDifference.MemberPath.ToCamelCase()}", propertyDifference.Changed!);
                                    hasChanges = true;
                                }
                            }
                        }
                        break;
                }
            }

            if (!hasChanges) return;

            var collection = _database.GetCollection<BsonDocument>(model.Name);
            await collection.UpdateOneAsync(filter, updateBuilder, new UpdateOptions { IsUpsert = true });

            await Task.CompletedTask;
        }
    }
}
