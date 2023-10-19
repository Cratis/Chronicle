// Copyright (c) Aksio Insurtech. All rights reserved.
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
using Aksio.Cratis.Kernel.Engines.Observation.Reducers;
using Aksio.Cratis.Kernel.Grains.Clients;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Observation.Reducers;
using Aksio.Cratis.Properties;
using Aksio.DependencyInversion;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Observation.Reducers.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientReducerSubscriber"/>.
/// </summary>
public class ClientReducerSubscriber : Grain, IClientReducerSubscriber
{
    readonly ILogger<ClientReducerSubscriber> _logger;
    readonly IHttpClientFactory _httpClientFactory;
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IReducerPipelineDefinitions> _reducerPipelineDefinitionsProvider;
    readonly IReducerPipelineFactory _reducerPipelineFactory;
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
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    /// <param name="httpClientFactory"><see cref="IHttpClientFactory"/> for connecting to the client.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="reducerPipelineDefinitionsProvider"><see cref="IReducerPipelineDefinitions"/> to get pipeline definitions from.</param>
    /// <param name="reducerPipelineFactory"><see cref="IReducerPipelineFactory"/> for creating instances of <see cref="IReducerPipeline"/>.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between JSON and <see cref="ExpandoObject"/>.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    public ClientReducerSubscriber(
        ILogger<ClientReducerSubscriber> logger,
        IHttpClientFactory httpClientFactory,
        IExecutionContextManager executionContextManager,
        ProviderFor<IReducerPipelineDefinitions> reducerPipelineDefinitionsProvider,
        IReducerPipelineFactory reducerPipelineFactory,
        IExpandoObjectConverter expandoObjectConverter,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _executionContextManager = executionContextManager;
        _reducerPipelineDefinitionsProvider = reducerPipelineDefinitionsProvider;
        _reducerPipelineFactory = reducerPipelineFactory;
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

        _executionContextManager.Establish(_tenantId, _executionContextManager.Current.CorrelationId, _microserviceId);
        var definition = await _reducerPipelineDefinitionsProvider().GetFor(_reducerId);
        _pipeline = await _reducerPipelineFactory.CreateFrom(definition);
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

        if (context.Metadata is JsonElement connectedClientJsonObject)
        {
            var connectedClient = connectedClientJsonObject.Deserialize<ConnectedClient>(_jsonSerializerOptions);
            if (connectedClient is not null)
            {
                using var httpClient = _httpClientFactory.CreateClient(ConnectedClients.ConnectedClientsHttpClient);
                httpClient.BaseAddress = connectedClient.ClientUri;
                var state = ObserverSubscriberState.Ok;
                var commandResult = CommandResult.Success;

                var isReplaying = events.Any(_ => _.Context.ObservationState.HasFlag(EventObservationState.Replay));
                var isHeadOfReplay = events.Any(_ => _.Context.ObservationState.HasFlag(EventObservationState.HeadOfReplay));
                var isTailOfReplay = events.Any(_ => _.Context.ObservationState.HasFlag(EventObservationState.TailOfReplay));

                var observationState = EventObservationState.None;
                if (isReplaying) observationState |= EventObservationState.Replay;
                if (isHeadOfReplay) observationState |= EventObservationState.HeadOfReplay;
                if (isTailOfReplay) observationState |= EventObservationState.TailOfReplay;

                var firstEvent = events.First();
                var reducerContext = new ReducerContext(
                    events,
                    new Key(firstEvent.Context.EventSourceId, ArrayIndexers.NoIndexers),
                    observationState);

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
                        var result = jsonElement.Deserialize<ReduceResult>(_jsonSerializerOptions)!;
                        return _expandoObjectConverter.ToExpandoObject(result.State ?? new JsonObject(), _pipeline.ReadModel.Schema);
                    }

                    return new ExpandoObject();
                }) ?? Task.CompletedTask);

                return new ObserverSubscriberResult(state, EventSequenceNumber.Unavailable, commandResult.ExceptionMessages, commandResult.ExceptionStackTrace);
            }
        }

        return new ObserverSubscriberResult(ObserverSubscriberState.Disconnected, EventSequenceNumber.Unavailable, Enumerable.Empty<string>(), string.Empty);
    }
}
