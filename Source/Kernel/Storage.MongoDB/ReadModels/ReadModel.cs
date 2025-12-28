// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Projections;

/// <summary>
/// Represents a read model.
/// </summary>
/// <param name="Id">The unique identifier of the read model.</param>
/// <param name="Name">The friendly display name of the read model.</param>
/// <param name="Owner">The owner of the read model.</param>
/// <param name="Sink">The sink definition for the read model.</param>
/// <param name="Schemas">The schemas per generation of the read model.</param>
public record ReadModel(
    ReadModelIdentifier Id,
    ReadModelName Name,
    ReadModelOwner Owner,
    SinkDefinition Sink,
    IDictionary<string, BsonDocument> Schemas);
