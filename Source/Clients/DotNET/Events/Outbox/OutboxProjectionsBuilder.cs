// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Events.Projections;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.Outbox;
using Aksio.Cratis.Guids;
using Aksio.Cratis.Schemas;

namespace Aksio.Cratis.Events.Outbox;

/// <summary>
/// Represents an implementation of <see cref="IOutboxProjectionsBuilder"/>.
/// </summary>
public class OutboxProjectionsBuilder : IOutboxProjectionsBuilder
{
    readonly Dictionary<EventType, ProjectionDefinition> _projectionDefinitionsPerEventType = new();
    readonly IEventTypes _eventTypes;
    readonly IJsonSchemaGenerator _jsonSchemaGenerator;
    readonly ProjectionId _projectionId;
    readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxProjectionsBuilder"/> class.
    /// </summary>
    /// <param name="eventTypes">Registered <see cref="IEventTypes"/>.</param>
    /// <param name="jsonSchemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating schemas for projections.</param>
    /// <param name="projectionId">The root projectionId.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    public OutboxProjectionsBuilder(
        IEventTypes eventTypes,
        IJsonSchemaGenerator jsonSchemaGenerator,
        ProjectionId projectionId,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _eventTypes = eventTypes;
        _jsonSchemaGenerator = jsonSchemaGenerator;
        _projectionId = projectionId;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    /// <inheritdoc/>
    public IOutboxProjectionsBuilder For<TTargetEvent>(Action<IProjectionBuilderFor<TTargetEvent>> projectionBuilderCallback)
    {
        var eventClrType = typeof(TTargetEvent);
        if (!_eventTypes.HasFor(eventClrType))
        {
            throw new MissingEventTypeAttribute(eventClrType);
        }

        var eventType = _eventTypes.GetEventTypeFor(eventClrType);
        if (!eventType.IsPublic)
        {
            throw new EventTypeNeedsToBeMarkedPublic(eventClrType);
        }

        var identifier = _projectionId.Value.Xor(eventType.Id.Value);
        var projectionBuilder = new ProjectionBuilderFor<TTargetEvent>(identifier, _eventTypes, _jsonSchemaGenerator, _jsonSerializerOptions);
        projectionBuilderCallback(projectionBuilder);
        projectionBuilder.WithName($"Outbox for ${eventClrType.FullName}");
        _projectionDefinitionsPerEventType[eventType] = projectionBuilder.Build();

        return this;
    }

    /// <inheritdoc/>
    public OutboxProjectionsDefinition Build() => new(_projectionDefinitionsPerEventType);
}
