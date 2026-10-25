// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema;

/// <summary>
/// Mirrors the internal job indexes in Cratis.Orleans.Storage.MongoDB 1.27.4, pinned by a drift spec.
/// </summary>
internal static class MongoDBJobIndexes
{
    /// <summary>
    /// Gets the indexes on jobs.
    /// </summary>
    internal static IReadOnlyList<CreateIndexModel<BsonDocument>> Jobs { get; } =
    [
        new(new BsonDocument("status", 1), new CreateIndexOptions { Name = "status", Background = true }),
        new(new BsonDocument { { "type", 1 }, { "status", 1 } }, new CreateIndexOptions { Name = "type_status", Background = true }),
        new(new BsonDocument { { "status", 1 }, { "created", 1 }, { "_id", 1 } }, new CreateIndexOptions { Name = "status_created", Background = true }),
        new(new BsonDocument { { "type", 1 }, { "status", 1 }, { "created", 1 }, { "_id", 1 } }, new CreateIndexOptions { Name = "type_status_created", Background = true })
    ];

    /// <summary>
    /// Gets the indexes on both active and failed job steps.
    /// </summary>
    internal static IReadOnlyList<CreateIndexModel<BsonDocument>> Steps { get; } =
    [
        new(new BsonDocument("_id.jobId", 1), new CreateIndexOptions { Name = "jobId", Background = true })
    ];
}
