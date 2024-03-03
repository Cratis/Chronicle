// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Kernel.Storage.MongoDB.Sinks;

/// <summary>
/// Represents a MongoDB property.
/// </summary>
/// <param name="Property">The name of the property.</param>
/// <param name="ArrayFilters">Collection of array filters.</param>
public record MongoDBProperty(string Property, IEnumerable<BsonDocumentArrayFilterDefinition<BsonDocument>> ArrayFilters);
