// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Sinks;

/// <summary>
/// Represents a MongoDB property.
/// </summary>
/// <param name="Property">The name of the property.</param>
/// <param name="ArrayFilters">Collection of array filters.</param>
public record MongoDBProperty(string Property, IEnumerable<BsonDocumentArrayFilterDefinition<BsonDocument>> ArrayFilters);
