// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Sinks;

/// <summary>
/// Represents a MongoDB update definition and accompanying array filters.
/// </summary>
/// <param name="UpdateDefinition">The actual update definition.</param>
/// <param name="ArrayFilters">Any array filters associated.</param>
/// <param name="hasChanges">Whether or not there are changes.</param>
public record MongoDBUpdateDefinitionAndArrayFilters(UpdateDefinition<BsonDocument> UpdateDefinition, BsonDocumentArrayFilterDefinition<BsonDocument>[] ArrayFilters, bool hasChanges);
