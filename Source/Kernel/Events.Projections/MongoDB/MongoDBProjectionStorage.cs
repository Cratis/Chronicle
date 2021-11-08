// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Changes;
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
        public async Task ApplyChanges(Model model, object key, Changeset<Event> changeset)
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

            bool UpdateChangedProperties(IEnumerable<PropertyDifference> differences, string? prefix = default)
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
                    case PropertiesChanged propertiesChanged:
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

                    case ChildPropertiesChanged childPropertiesChanged:
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

            var collection = _database.GetCollection<BsonDocument>(model.Name);
            await collection.UpdateOneAsync(filter, updateBuilder, new UpdateOptions { IsUpsert = true });

            await Task.CompletedTask;
        }
    }
}
