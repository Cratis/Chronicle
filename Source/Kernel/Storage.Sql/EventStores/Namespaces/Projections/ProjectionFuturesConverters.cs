// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Projections;

/// <summary>
/// Converters for projection futures to and from storage.
/// </summary>
public static class ProjectionFuturesConverters
{
    /// <summary>
    /// Convert <see cref="ProjectionFuture"/> to <see cref="ProjectionFutureEntity"/>.
    /// </summary>
    /// <param name="projectionId">The projection identifier.</param>
    /// <param name="future">The <see cref="ProjectionFuture"/> to convert.</param>
    /// <param name="jsonSerializerOptions">The JSON serializer options.</param>
    /// <returns>The converted <see cref="ProjectionFutureEntity"/>.</returns>
    public static ProjectionFutureEntity ToEntity(ProjectionId projectionId, ProjectionFuture future, JsonSerializerOptions jsonSerializerOptions) =>
        new()
        {
            Id = future.Id.Value.ToString(),
            ProjectionId = projectionId.Value,
            EventSequenceNumber = future.Event.Context.SequenceNumber.Value,
            EventTypeId = future.Event.Context.EventType.Id.Value,
            EventTypeGeneration = future.Event.Context.EventType.Generation.Value,
            EventSourceId = future.Event.Context.EventSourceId.Value,
            EventContentJson = JsonSerializer.Serialize(future.Event.Content, jsonSerializerOptions),
            ParentPath = future.ParentPath.Path,
            ChildPath = future.ChildPath.Path,
            IdentifiedByProperty = future.IdentifiedByProperty.Path,
            ParentIdentifiedByProperty = future.ParentIdentifiedByProperty.Path,
            ParentKeyJson = JsonSerializer.Serialize(future.ParentKey.Value, jsonSerializerOptions),
            Created = future.Created
        };

    /// <summary>
    /// Convert <see cref="ProjectionFutureEntity"/> to <see cref="ProjectionFuture"/>.
    /// </summary>
    /// <param name="entity">The <see cref="ProjectionFutureEntity"/> to convert.</param>
    /// <param name="jsonSerializerOptions">The JSON serializer options.</param>
    /// <returns>The converted <see cref="ProjectionFuture"/>.</returns>
    public static ProjectionFuture ToProjectionFuture(ProjectionFutureEntity entity, JsonSerializerOptions jsonSerializerOptions)
    {
        var content = JsonSerializer.Deserialize<ExpandoObject>(entity.EventContentJson, jsonSerializerOptions)!;
        var parentKeyValue = JsonSerializer.Deserialize<object>(entity.ParentKeyJson, jsonSerializerOptions)!;

        var appendedEvent = new AppendedEvent(
            EventContext.Empty with
            {
                SequenceNumber = new EventSequenceNumber(entity.EventSequenceNumber),
                EventType = new EventType(new EventTypeId(entity.EventTypeId), entity.EventTypeGeneration),
                EventSourceId = new EventSourceId(entity.EventSourceId)
            },
            content);

        return new ProjectionFuture(
            new ProjectionFutureId(Guid.Parse(entity.Id)),
            new ProjectionId(entity.ProjectionId),
            appendedEvent,
            new PropertyPath(entity.ParentPath),
            new PropertyPath(entity.ChildPath),
            new PropertyPath(entity.IdentifiedByProperty),
            new PropertyPath(entity.ParentIdentifiedByProperty),
            new Key(parentKeyValue, ArrayIndexers.NoIndexers),
            entity.Created);
    }
}
