// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Commands;
using Aksio.Cratis.Connections;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Json;
using Aksio.Cratis.Kernel.Grains.Clients;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Observation.Reducers;
using Aksio.Cratis.Properties;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Observation.Reducers.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientReducerSubscriber"/>.
/// </summary>
public class ClientReducerSubscriber : Grain, IClientReducerSubscriber
{
    readonly ILogger<ClientReducerSubscriber> _logger;
    readonly IKernel _kernel;
    readonly IHttpClientFactory _httpClientFactory;
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    MicroserviceId _microserviceId = MicroserviceId.Unspecified;
    ReducerId _reducerId = ObserverId.Unspecified;
    TenantId _tenantId = TenantId.NotSet;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;
    IConnectedClients? _connectedClients;
    IReducerPipeline? _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientReducerSubscriber"/> class.
    /// </summary>
    /// <param name="kernel"><see cref="IKernel"/> for accessing global artifacts.</param>
    /// <param name="httpClientFactory"><see cref="IHttpClientFactory"/> for connecting to the client.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between JSON and <see cref="ExpandoObject"/>.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ClientReducerSubscriber(
        IKernel kernel,
        IHttpClientFactory httpClientFactory,
        IExpandoObjectConverter expandoObjectConverter,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<ClientReducerSubscriber> logger)
    {
        _logger = logger;
        _kernel = kernel;
        _httpClientFactory = httpClientFactory;
        _expandoObjectConverter = expandoObjectConverter;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    IConnectedClients ConnectedClientsGrain => _connectedClients ??= GrainFactory.GetGrain<IConnectedClients>(_microserviceId);

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var id = this.GetPrimaryKey(out var keyAsString);
        var key = ObserverSubscriberKey.Parse(keyAsString);
        _microserviceId = key.MicroserviceId;
        _reducerId = id;
        _tenantId = key.TenantId;
        _eventSequenceId = key.EventSequenceId;

        var eventStore = _kernel.GetEventStore((string)_microserviceId);
        var eventStoreNamespace = eventStore.GetNamespace(_tenantId);

        var definition = await eventStore.ReducerPipelineDefinitions.GetFor(_reducerId);
        _pipeline = await eventStoreNamespace.ReducerPipelines.GetFor(definition);
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        foreach (var @event in events)
        {
            _logger.EventReceived(
                _reducerId.Value,
                _microserviceId,
                _tenantId,
                @event.Metadata.Type.Id,
                _eventSequenceId,
                @event.Context.SequenceNumber);
        }

        if (context.Metadata is ConnectedClient connectedClient)
        {
            using var httpClient = _httpClientFactory.CreateClient(ConnectedClients.ConnectedClientsHttpClient);
            httpClient.BaseAddress = connectedClient.ClientUri;
            var state = ObserverSubscriberState.Ok;
            var commandResult = CommandResult.Success;

            var firstEvent = events.First();
            var reducerContext = new ReducerContext(
                events,
                new Key(firstEvent.Context.EventSourceId, ArrayIndexers.NoIndexers));

            var lastSuccessfullyObservedEvent = EventSequenceNumber.Unavailable;

            ReduceResult? reduceResult = null;

            await (_pipeline?.Handle(reducerContext, async (_, initial) =>
            {
                var reduce = new Reduce(
                    events,
                    initial is not null ? _expandoObjectConverter.ToJsonObject(initial, _pipeline.ReadModel.Schema) : null);
                using var jsonContent = JsonContent.Create(reduce, options: _jsonSerializerOptions);
                httpClient.DefaultRequestHeaders.Add(ExecutionContextAppBuilderExtensions.TenantIdHeader, _tenantId.ToString());
                var response = await httpClient.PostAsync($"/.cratis/reducers/{_reducerId}", jsonContent);

                var contentAsString = string.Empty;

                try
                {
                    contentAsString = await response.Content.ReadAsStringAsync();
                    commandResult = JsonSerializer.Deserialize<CommandResult>(contentAsString, _jsonSerializerOptions)!;
                }
                catch
                {
                    throw new InvalidReturnContentFromReducer(response.StatusCode, response.ReasonPhrase ?? "[n/a]", contentAsString);
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    await ConnectedClientsGrain.OnClientDisconnected(connectedClient.ConnectionId, "Client not found");
                    state = ObserverSubscriberState.Disconnected;
                }
                else if (response.StatusCode != HttpStatusCode.OK || !commandResult.IsSuccess)
                {
                    state = ObserverSubscriberState.Failed;
                }

                if (commandResult.Response is not null && commandResult.Response is JsonElement jsonElement)
                {
                    reduceResult = jsonElement.Deserialize<ReduceResult>(_jsonSerializerOptions)!;
                    return _expandoObjectConverter.ToExpandoObject(reduceResult.State ?? new JsonObject(), _pipeline.ReadModel.Schema);
                }

                return new ExpandoObject();
            }) ?? Task.CompletedTask);

#pragma warning disable CA1508 // Avoid dead conditional code - false positive
            if (reduceResult is null)
            {
                return new ObserverSubscriberResult(state, EventSequenceNumber.Unavailable, Enumerable.Empty<string>(), string.Empty);
            }
#pragma warning restore CA1508 // Avoid dead conditional code

            return new ObserverSubscriberResult(state, reduceResult.LastSuccessfullyObservedEvent, reduceResult.ErrorMessages, reduceResult.StackTrace);
        }

        return new ObserverSubscriberResult(ObserverSubscriberState.Disconnected, EventSequenceNumber.Unavailable, Enumerable.Empty<string>(), string.Empty);
    }
}
