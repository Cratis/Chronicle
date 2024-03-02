// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Sinks;

/// <summary>
/// Represents a MongoDB update definition and accompanying array filters.
/// </summary>
/// <param name="UpdateDefinition">The actual update definition.</param>
/// <param name="ArrayFilters">Any array filters associated.</param>
/// <param name="hasChanges">Whether or not there are changes.</param>
public record UpdateDefinitionAndArrayFilters(UpdateDefinition<BsonDocument> UpdateDefinition, IEnumerable<BsonDocumentArrayFilterDefinition<BsonDocument>> ArrayFilters, bool hasChanges);
