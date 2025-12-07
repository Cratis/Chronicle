// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelProjections;

using System.Dynamic;
using System.Reactive.Linq;
using System.Text.Json;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Grains;
using Cratis.Chronicle.Grains.Projections;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Services.Events;
using Cratis.Chronicle.Services.Projections.Definitions;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using KernelProjections::Cratis.Chronicle.Projections;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Streams;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjections"/>.
/// </summary>
/// <param name="clusterClient"><see cref="IClusterClient"/> for interacting with the cluster.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating projections.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting ExpandoObjects.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for accessing services.</param>
/// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
internal sealed class Projections(
    IClusterClient clusterClient,
    IGrainFactory grainFactory,
    IProjectionFactory projectionFactory,
    IObjectComparer objectComparer,
    IExpandoObjectConverter expandoObjectConverter,
    IServiceProvider serviceProvider,
    JsonSerializerOptions jsonSerializerOptions) : Contracts.Projections.IProjections
{
    /// <inheritdoc/>
    public Task Register(RegisterRequest request, CallContext context = default)
    {
        var projectionsManager = grainFactory.GetGrain<Grains.Projections.IProjectionsManager>(request.EventStore);
        var projections = request.Projections.Select(_ => _.ToChronicle((Concepts.Projections.ProjectionOwner)(int)request.Owner)).ToArray();

        _ = Task.Run(() => projectionsManager.Register(projections));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<Contracts.Projections.ProjectionResult> GetInstanceById(GetInstanceByIdRequest request, CallContext context = default)
    {
        var projectionKey = new ImmediateProjectionKey(
            request.ProjectionId,
            request.EventStore,
            request.Namespace,
            request.EventSequenceId,
            request.ReadModelKey);

        var projection = grainFactory.GetGrain<IImmediateProjection>(projectionKey);

        var result = await projection.GetModelInstance();
        return result.ToContract(jsonSerializerOptions);
    }

    /// <inheritdoc/>
    public async Task<Contracts.Projections.ProjectionResult> GetInstanceByIdForSession(GetInstanceByIdForSessionRequest request, CallContext context = default)
    {
        var projectionKey = new ImmediateProjectionKey(
            request.ProjectionId,
            request.EventStore,
            request.Namespace,
            request.EventSequenceId,
            request.ReadModelKey,
            request.SessionId);

        var projection = grainFactory.GetGrain<IImmediateProjection>(projectionKey);

        var result = await projection.GetModelInstance();
        return result.ToContract(jsonSerializerOptions);
    }

    /// <inheritdoc/>
    public async Task<Contracts.Projections.ProjectionResult> GetInstanceByIdForSessionWithEventsApplied(GetInstanceByIdForSessionWithEventsAppliedRequest request, CallContext context = default)
    {
        var projectionKey = new ImmediateProjectionKey(
            request.ProjectionId,
            request.EventStore,
            request.Namespace,
            request.EventSequenceId,
            request.ReadModelKey,
            request.SessionId);

        var projection = grainFactory.GetGrain<IImmediateProjection>(projectionKey);

        var eventsToApply = request.Events.Select(_ => _.ToChronicle()).ToArray();
        var result = await projection.GetCurrentModelInstanceWithAdditionalEventsApplied(eventsToApply);
        return result.ToContract(jsonSerializerOptions);
    }

    /// <inheritdoc/>
    public IObservable<Contracts.Projections.ProjectionChangeset> Watch(ProjectionWatchRequest request, CallContext context = default)
    {
        var streamProvider = clusterClient.GetStreamProvider(WellKnownStreamProviders.ProjectionChangesets);
        var streamId = StreamId.Create(new StreamIdentity(Guid.Empty, request.ProjectionId));

        var stream = streamProvider.GetStream<Grains.Projections.ProjectionChangeset>(streamId);
        StreamSubscriptionHandle<Grains.Projections.ProjectionChangeset>? subscription = null;

        var observable = Observable.Create<Contracts.Projections.ProjectionChangeset>(async (observer, cancellationToken) =>
        {
            subscription = await stream.SubscribeAsync((changeset, _) =>
            {
                observer.OnNext(new Contracts.Projections.ProjectionChangeset
                {
                    Namespace = changeset.Namespace,
                    ReadModelKey = changeset.ReadModelKey,
                    ReadModel = changeset.ReadModel.ToJsonString(jsonSerializerOptions)
                });

                return Task.CompletedTask;
            });

            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        });

        context.CancellationToken.Register(() => subscription?.UnsubscribeAsync().GetAwaiter().GetResult());
        return observable;
    }

    /// <inheritdoc/>
    public async Task DehydrateSession(DehydrateSessionRequest request, CallContext context = default)
    {
        var projectionKey = new ImmediateProjectionKey(
            request.ProjectionId,
            request.EventStore,
            request.Namespace,
            request.EventSequenceId,
            request.ReadModelKey,
            request.SessionId);

        var projection = grainFactory.GetGrain<IImmediateProjection>(projectionKey);
        await projection.Dehydrate();
    }

    /// <inheritdoc/>
    public async Task<GetSnapshotsByIdResponse> GetSnapshotsById(GetSnapshotsByIdRequest request, CallContext context = default)
    {
        var snapshots = await GetSnapshotsForProjection(
            request.ProjectionId,
            request.EventStore,
            request.Namespace,
            request.EventSequenceId,
            request.ReadModelKey);

        return new GetSnapshotsByIdResponse
        {
            Snapshots = snapshots.ToList()
        };
    }

    async Task<IEnumerable<ProjectionSnapshot>> GetSnapshotsForProjection(
        string projectionId,
        string eventStoreName,
        string namespaceName,
        string eventSequenceId,
        string readModelKey)
    {
        var storage = serviceProvider.GetRequiredService<IStorage>();
        var eventSequenceStorage = storage
            .GetEventStore(eventStoreName)
            .GetNamespace(namespaceName)
            .GetEventSequence(eventSequenceId);

        var projectionKey = new ProjectionKey(projectionId, eventStoreName);
        var projection = grainFactory.GetGrain<Grains.Projections.IProjection>(projectionKey);
        var definition = await projection.GetDefinition();
        var readModelDefinition = await storage.GetEventStore(eventStoreName).ReadModels.Get(definition.ReadModel);
        var projectionInstance = await projectionFactory.Create(eventStoreName, namespaceName, definition, readModelDefinition);

        // For now, we'll get all events - in the future we could filter by projection event types
        var tailSequenceNumber = await eventSequenceStorage.GetTailSequenceNumber();

        if (tailSequenceNumber == EventSequenceNumber.Unavailable)
        {
            return [];
        }

        var cursor = await eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, readModelKey);

        var eventsByCorrelation = new Dictionary<Guid, List<AppendedEvent>>();

        while (await cursor.MoveNext())
        {
            foreach (var appendedEvent in cursor.Current)
            {
                var correlationId = appendedEvent.Context.CorrelationId;
                if (!eventsByCorrelation.ContainsKey(correlationId))
                {
                    eventsByCorrelation[correlationId] = [];
                }
                eventsByCorrelation[correlationId].Add(appendedEvent);
            }
        }

        var snapshots = new List<ProjectionSnapshot>();

        foreach (var (correlationId, events) in eventsByCorrelation)
        {
            var orderedEvents = events.OrderBy(e => e.Context.SequenceNumber).ToList();
            var firstOccurred = orderedEvents[0].Context.Occurred;

            // Serialize events as AppendedEvent array with context and content
            var eventsJson = JsonSerializer.Serialize(
                orderedEvents.Select(e => new Contracts.Events.AppendedEvent
                {
                    Context = e.Context.ToContract(),
                    Content = JsonSerializer.Serialize(e.Content, jsonSerializerOptions)
                }).ToArray(),
                jsonSerializerOptions);

            var initialState = new ExpandoObject();
            var result = await HandleEvents(projectionInstance, eventSequenceStorage, initialState, orderedEvents.ToArray());
            var jsonObject = expandoObjectConverter.ToJsonObject(result, projectionInstance.ReadModel.GetSchemaForLatestGeneration());
            var readModel = JsonSerializer.Serialize(jsonObject, jsonSerializerOptions);

            snapshots.Add(new ProjectionSnapshot
            {
                ReadModel = readModel,
                Events = eventsJson,
                Occurred = firstOccurred!,
                CorrelationId = correlationId
            });
        }

        cursor.Dispose();
        return snapshots;
    }

    async Task<ExpandoObject> HandleEvents(
        KernelProjections::Cratis.Chronicle.Projections.IProjection projection,
        IEventSequenceStorage eventSequenceStorage,
        ExpandoObject initialState,
        AppendedEvent[] events)
    {
        var state = initialState;
        var eventSequenceNumber = EventSequenceNumber.Unavailable;

        foreach (var @event in events)
        {
            var changeset = new Changeset<AppendedEvent, ExpandoObject>(objectComparer, @event, state);
            var keyResolver = projection!.GetKeyResolverFor(@event.Context.EventType);
            var keyResult = await keyResolver(eventSequenceStorage!, NullSink.Instance, @event);

            // Skip deferred keys in immediate projections - they need parent data that's not yet available
            if (keyResult is DeferredKey)
            {
                continue;
            }

            var key = (keyResult as ResolvedKey)!.Key;
            var context = new ProjectionEventContext(
                key,
                @event,
                changeset,
                projection.GetOperationTypeFor(@event.Context.EventType),
                false);

            await HandleEventFor(projection!, context);

            eventSequenceNumber = @event.Context.SequenceNumber;

            state = ApplyActualChanges(key, changeset.Changes, changeset.InitialState);
        }

        return state;
    }

    async Task HandleEventFor(KernelProjections::Cratis.Chronicle.Projections.IProjection projection, ProjectionEventContext context)
    {
        if (projection.Accepts(context.Event.Context.EventType))
        {
            projection.OnNext(context);
        }

        foreach (var child in projection.ChildProjections)
        {
            await HandleEventFor(child, context);
        }
    }

    ExpandoObject ApplyActualChanges(Key key, IEnumerable<Change> changes, ExpandoObject state)
    {
        foreach (var change in changes)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject> propertiesChanged:
                    state = state.MergeWith((change.State as ExpandoObject)!);
                    break;

                case ChildAdded childAdded:
                    var items = state.EnsureCollection<object>(childAdded.ChildrenProperty, key.ArrayIndexers);
                    items.Add(childAdded.Child);
                    break;

                case Joined joined:
                    state = ApplyActualChanges(key, joined.Changes, state);
                    break;

                case ResolvedJoin resolvedJoin:
                    state = ApplyActualChanges(key, resolvedJoin.Changes, state);
                    break;
            }
        }

        return state;
    }
}
