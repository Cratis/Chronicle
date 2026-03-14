// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Events.EventTypes;

/// <summary>
/// Represents a migration definition between two event type generations for MongoDB storage.
/// </summary>
/// <param name="FromGeneration">The generation to migrate from.</param>
/// <param name="ToGeneration">The generation to migrate to.</param>
/// <param name="UpcastJmesPath">The JmesPath expression for upcast migration (From → To).</param>
/// <param name="DowncastJmesPath">The JmesPath expression for downcast migration (To → From).</param>
public record EventTypeMigration(
    EventTypeGeneration FromGeneration,
    EventTypeGeneration ToGeneration,
    BsonDocument UpcastJmesPath,
    BsonDocument DowncastJmesPath);
