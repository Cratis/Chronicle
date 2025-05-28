// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventSerializer"/>.
/// </summary>
[Singleton]
public class EventSerializer : IEventSerializer
{
    readonly IEnumerable<ICanProvideAdditionalEventInformation> _additionalEventInformationProviders;
    readonly IEventTypes _eventTypes;
    readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSerializer"/> class.
    /// </summary>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for resolving instances.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for resolving event types.</param>
    /// <param name="serializerOptions">The common <see creF="JsonSerializerOptions"/>.</param>
    public EventSerializer(
        IClientArtifactsProvider clientArtifacts,
        IServiceProvider serviceProvider,
        IEventTypes eventTypes,
        JsonSerializerOptions serializerOptions)
    {
        _eventTypes = eventTypes;
        _serializerOptions = new JsonSerializerOptions(serializerOptions)
        {
            Converters =
            {
                new EventRedactedConverters(eventTypes)
            }
        };
        _additionalEventInformationProviders = clientArtifacts.AdditionalEventInformationProviders
            .Select(type => serviceProvider.GetRequiredService(type) as ICanProvideAdditionalEventInformation)
            .Where(provider => provider != null)!;
    }

    /// <inheritdoc/>
    public Task<object> Deserialize(Type type, JsonObject json) => Task.FromResult(json.Deserialize(type, _serializerOptions)!);

    /// <inheritdoc/>
    public async Task<JsonObject> Serialize(object @event)
    {
        var eventAsJson = (JsonSerializer.SerializeToNode(@event, _serializerOptions) as JsonObject)!;

        foreach (var provider in _additionalEventInformationProviders)
        {
            await provider.ProvideFor(eventAsJson);
        }

        return eventAsJson;
    }

    /// <inheritdoc/>
    public async Task<object> Deserialize(Type type, ExpandoObject expandoObject)
    {
        var json = await Serialize(expandoObject);
        return await Deserialize(type, json);
    }

    /// <inheritdoc/>
    public async Task<object> Deserialize(AppendedEvent @event)
    {
        var eventType = _eventTypes.GetClrTypeFor(@event.Metadata.Type.Id);

        var json = await Serialize(@event.Content);
        return await Deserialize(eventType, json);
    }
}
