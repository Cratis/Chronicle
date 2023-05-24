// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Types;

namespace Aksio.Cratis.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventSerializer"/>.
/// </summary>
[Singleton]
public class EventSerializer : IEventSerializer
{
    readonly IInstancesOf<ICanProvideAdditionalEventInformation> _additionalEventInformationProviders;
    readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSerializer"/> class.
    /// </summary>
    /// <param name="additionalEventInformationProviders">Providers of additional event information.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for resolving event types.</param>
    /// <param name="serializerOptions">The common <see creF="JsonSerializerOptions"/>.</param>
    public EventSerializer(
        IInstancesOf<ICanProvideAdditionalEventInformation> additionalEventInformationProviders,
        IEventTypes eventTypes,
        JsonSerializerOptions serializerOptions)
    {
        _additionalEventInformationProviders = additionalEventInformationProviders;
        _serializerOptions = new JsonSerializerOptions(serializerOptions)
        {
            Converters =
            {
                new EventRedactedConverter(eventTypes)
            }
        };
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
}
