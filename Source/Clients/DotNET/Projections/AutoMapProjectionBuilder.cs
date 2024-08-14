// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Schemas;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents a builder for building out projections that will automatically have properties mapped.
/// </summary>
/// <param name="addDefinitions">Action to add definitions to.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for providing event type information.</param>
/// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
/// <typeparam name="TModel">Type of model to build for.</typeparam>
public class AutoMapProjectionBuilder<TModel>(Action<Dictionary<EventType, FromDefinition>> addDefinitions, IEventTypes eventTypes, IJsonSchemaGenerator schemaGenerator, JsonSerializerOptions jsonSerializerOptions)
    : ProjectionBuilder<TModel, AutoMapProjectionBuilder<TModel>>(eventTypes, schemaGenerator, jsonSerializerOptions)
{
    /// <inheritdoc/>
    public override AutoMapProjectionBuilder<TModel> From<TEvent>(Action<IFromBuilder<TModel, TEvent>>? builderCallback = default)
    {
        base.From<TEvent>(builder =>
        {
            builder.AutoMap();
            builderCallback?.Invoke(builder);
            addDefinitions(_fromDefinitions);
            _fromDefinitions.Clear();
        });

        return this;
    }
}
