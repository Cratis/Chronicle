// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventSerializer"/>.
/// </summary>
[Singleton]
public class EventSerializer : IEventSerializer
{
    readonly ICanProvideAdditionalEventInformation[] _additionalEventInformationProviders;
    readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSerializer"/> class.
    /// </summary>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    /// <param name="artifactActivator"><see cref="IServiceProvider"/> for resolving instances.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for resolving event types.</param>
    /// <param name="serializerOptions">The common <see creF="JsonSerializerOptions"/>.</param>
    public EventSerializer(
        IClientArtifactsProvider clientArtifacts,
        IClientArtifactsActivator artifactActivator,
        IEventTypes eventTypes,
        JsonSerializerOptions serializerOptions)
    {
        _serializerOptions = new JsonSerializerOptions(serializerOptions)
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new EventRedactedConverters(eventTypes)
            }
        };

        var providers = new List<ICanProvideAdditionalEventInformation>();
        foreach (var providerType in clientArtifacts.AdditionalEventInformationProviders)
        {
            var activated = artifactActivator.ActivateNonDisposable<ICanProvideAdditionalEventInformation>(providerType);
            if (activated.IsT0 && activated.AsT0 is not null)
            {
                providers.Add(activated.AsT0);
            }
        }

        _additionalEventInformationProviders = [.. providers];
    }

    /// <inheritdoc/>
    public Task<object> Deserialize(Type type, JsonObject json) => Task.FromResult(json.Deserialize(type, _serializerOptions)!);

    /// <inheritdoc/>
    public Task<JsonObject> Serialize(object @event)
    {
        var eventAsJson = (JsonSerializer.SerializeToNode(@event, _serializerOptions) as JsonObject)!;

        if (_additionalEventInformationProviders.Length == 0)
        {
            return Task.FromResult(eventAsJson);
        }

        return ProvideAdditionalInformationFor(eventAsJson);
    }

    /// <inheritdoc/>
    public Task<object> Deserialize(AppendedEvent @event)
    {
        // Content is already deserialized to the correct type
        return Task.FromResult(@event.Content);
    }

    async Task<JsonObject> ProvideAdditionalInformationFor(JsonObject eventAsJson)
    {
        foreach (var provider in _additionalEventInformationProviders)
        {
            await provider.ProvideFor(eventAsJson).ConfigureAwait(false);
        }

        return eventAsJson;
    }
}
