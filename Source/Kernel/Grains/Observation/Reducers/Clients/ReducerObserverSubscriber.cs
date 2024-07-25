// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grains.Observation.Clients;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers;
using Cratis.Chronicle.Properties;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.Observation.Reducers.Clients;

/// <summary>
/// Represents an implementation of <see cref="IReducerObserverSubscriber"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReducerObserverSubscriber"/> class.
/// </remarks>
/// <param name="reducerPipelineFactory"><see cref="IReducerPipelineFactory"/> for creating pipelines.</param>
/// <param name="observerMediator"><see cref="IObserverMediator"/> for notifying actual clients.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Projections)]
public class ReducerObserverSubscriber(
    IReducerPipelineFactory reducerPipelineFactory,
    IObserverMediator observerMediator,
    ILogger<ReducerObserverSubscriber> logger) : Grain<ReducerDefinition>, IReducerObserverSubscriber, INotifyReducerDefinitionsChanged
{
    ObserverSubscriberKey _key = new(ObserverId.Unspecified, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, EventSequenceId.Unspecified, EventSourceId.Unspecified, string.Empty);
    EventStoreName _eventStore = EventStoreName.NotSet;
    ObserverId _observerId = ObserverId.Unspecified;
    EventStoreNamespaceName _namespace = EventStoreNamespaceName.NotSet;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;
    IReducerPipeline? _pipeline;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = ObserverSubscriberKey.Parse(this.GetPrimaryKeyString());
        _observerId = _key.ObserverId;
        _eventStore = _key.EventStore;
        _namespace = _key.Namespace;
        _eventSequenceId = _key.EventSequenceId;

        var reducer = GrainFactory.GetGrain<IReducer>(new ReducerKey(_key.ObserverId, _key.EventStore, _key.Namespace, _key.EventSequenceId));
        await reducer.SubscribeDefinitionsChanged(this.AsReference<INotifyReducerDefinitionsChanged>());

        await HandlePipeline();
    }

    /// <inheritdoc/>
    public void OnReducerDefinitionsChanged()
    {
        ReadStateAsync().Wait();
        HandlePipeline().Wait();
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        await Task.CompletedTask;

        foreach (var @event in events)
        {
            logger.EventReceived(
                _observerId,
                _eventStore,
                _namespace,
                @event.Metadata.Type.Id,
                _eventSequenceId,
                @event.Context.SequenceNumber);
        }

        if (context.Metadata is not ConnectedClient connectedClient)
        {
            throw new MissingStateForReducerSubscriber(_observerId);
        }

        var tcs = new TaskCompletionSource<ObserverSubscriberResult>();
        try
        {
            observerMediator.OnNext(
                _observerId,
                connectedClient.ConnectionId,
                events,
                tcs);

            var firstEvent = events.First();
            var reducerContext = new ReducerContext(
                events,
                new Key(firstEvent.Context.EventSourceId, ArrayIndexers.NoIndexers));

            _pipeline?.Handle(reducerContext, (events, initialState) =>
            {
                throw new NotImplementedException();
            });

            return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (TaskCanceledException)
        {
            return ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Task was cancelled");
        }
        catch (TimeoutException)
        {
            return ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Timeout");
        }

        /*
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
    }

    async Task HandlePipeline()
    {
        _pipeline = await reducerPipelineFactory.Create(_eventStore, _namespace, State);
    }
}
