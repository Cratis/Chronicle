// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Commands;
using Cratis.Connections;
using Cratis.Events;
using Cratis.EventSequences;
using Cratis.Json;
using Cratis.Kernel.Grains.Clients;
using Cratis.Kernel.Keys;
using Cratis.Observation;
using Cratis.Observation.Reducers;
using Cratis.Properties;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace Cratis.Kernel.Grains.Observation.Reducers.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientReducerSubscriber"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ClientReducerSubscriber"/> class.
/// </remarks>
/// <param name="kernel"><see cref="IKernel"/> for accessing global artifacts.</param>
/// <param name="httpClientFactory"><see cref="IHttpClientFactory"/> for connecting to the client.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between JSON and <see cref="ExpandoObject"/>.</param>
/// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class ClientReducerSubscriber(
    IKernel kernel,
    IHttpClientFactory httpClientFactory,
    IExpandoObjectConverter expandoObjectConverter,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<ClientReducerSubscriber> logger) : Grain, IClientReducerSubscriber
{
    readonly IKernel _kernel = kernel;
    EventStoreName _eventStore = EventStoreName.NotSet;
    ReducerId _reducerId = ObserverId.Unspecified;
    EventStoreNamespaceName _namespace = EventStoreNamespaceName.NotSet;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;
    IConnectedClients? _connectedClients;
    IReducerPipeline? _pipeline;

    IConnectedClients ConnectedClientsGrain => _connectedClients ??= GrainFactory.GetGrain<IConnectedClients>(0);

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var id = this.GetPrimaryKey(out var keyAsString);
        var key = ObserverSubscriberKey.Parse(keyAsString);
        _eventStore = key.EventStore;
        _reducerId = id;
        _namespace = key.Namespace;
        _eventSequenceId = key.EventSequenceId;

        var eventStore = _kernel.GetEventStore(_eventStore);
        var eventStoreNamespace = eventStore.GetNamespace(_namespace);

        var definition = await eventStore.ReducerPipelineDefinitions.GetFor(_reducerId);
        _pipeline = await eventStoreNamespace.ReducerPipelines.GetFor(definition);
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        await Task.CompletedTask;

        /*
        foreach (var @event in events)
        {
            _logger.EventReceived(
                _reducerId.Value,
                _eventStore,
                _namespace,
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
                httpClient.DefaultRequestHeaders.Add(ExecutionContextAppBuilderExtensions.TenantIdHeader, _namespace.ToString());
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
        */
        throw new NotImplementedException();
    }
}
