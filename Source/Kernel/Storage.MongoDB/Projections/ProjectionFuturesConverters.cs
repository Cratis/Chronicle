// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Projections;

/// <summary>
/// Conversion extensions for projection futures between Kernel and MongoDB storage models.
/// </summary>
public static class ProjectionFuturesConverters
{
    /// <summary>
    /// Converts a kernel projection future to the MongoDB projection future model.
    /// </summary>
    /// <param name="future">The kernel projection future.</param>
    /// <param name="jsonSerializerOptions">The JSON serializer options.</param>
    /// <returns>The MongoDB projection future.</returns>
    public static ProjectionFuture ToMongoDB(this Concepts.Projections.ProjectionFuture future, JsonSerializerOptions jsonSerializerOptions)
    {
        var content = BsonDocument.Parse(JsonSerializer.Serialize(future.Event.Content, jsonSerializerOptions));

        var @event = new Event(
            future.Event.Context.SequenceNumber,
            new(future.Event.Context.EventType.Id, future.Event.Context.EventType.Generation),
            future.Event.Context.EventSourceId,
            content);

        return new ProjectionFuture(
            future.Id,
            future.ProjectionId,
            @event,
            future.ParentPath,
            future.ChildPath,
            future.IdentifiedByProperty,
            future.ParentIdentifiedByProperty,
            future.ParentKey.Value,
            future.Created);
    }

    /// <summary>
    /// Converts a MongoDB projection future to the kernel projection future model.
    /// </summary>
    /// <param name="document">The MongoDB projection future.</param>
    /// <param name="jsonSerializerOptions">The JSON serializer options.</param>
    /// <returns>The kernel projection future.</returns>
    public static Concepts.Projections.ProjectionFuture ToKernel(this ProjectionFuture document, JsonSerializerOptions jsonSerializerOptions)
    {
        var contentJson = document.Event.Content.ToJson();
        var content = JsonSerializer.Deserialize<System.Dynamic.ExpandoObject>(contentJson, jsonSerializerOptions)!;

        var appendedEvent = new AppendedEvent(
            EventContext.Empty with
            {
                SequenceNumber = document.Event.EventSequenceId,
                EventType = new(document.Event.EventType.Id, document.Event.EventType.Generation),
                EventSourceId = document.Event.EventSourceId
            },
            content);

        return new Concepts.Projections.ProjectionFuture(
            document.Id,
            document.ProjectionId,
            appendedEvent,
            document.ParentPath,
            document.ChildPath,
            document.IdentifiedByProperty,
            document.ParentIdentifiedByProperty,
            new Key(document.ParentKey, ArrayIndexers.NoIndexers),
            document.Created);
    }
}
