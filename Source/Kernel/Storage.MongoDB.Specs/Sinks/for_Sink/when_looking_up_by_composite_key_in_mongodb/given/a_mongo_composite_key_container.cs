// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks.for_ISink.when_looking_up_by_composite_key.given;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_looking_up_by_composite_key_in_mongodb.given;

public abstract class a_mongo_composite_key_container : a_composite_key_read_model<MongoSinkHarness>
{
    protected MongoSinkHarness _mongo;
    protected IMongoCollection<BsonDocument> Collection => _mongo.Database.GetCollection<BsonDocument>(ContainerName);
}
