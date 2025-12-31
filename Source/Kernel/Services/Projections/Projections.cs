// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Grains;
using Cratis.Chronicle.Grains.Projections;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Services.Events;
using Cratis.Chronicle.Services.Projections.Definitions;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Streams;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjections"/>.
/// </summary>
/// <param name="clusterClient"><see cref="IClusterClient"/> for interacting with the cluster.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting ExpandoObjects.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for accessing services.</param>
/// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
internal sealed class Projections(
    IClusterClient clusterClient,
    IGrainFactory grainFactory,
    IExpandoObjectConverter expandoObjectConverter,
    IServiceProvider serviceProvider,
    JsonSerializerOptions jsonSerializerOptions) : IProjections
{
    /// <inheritdoc/>
    public Task Register(RegisterRequest request, CallContext context = default)
    {
        var projectionsManager = grainFactory.GetGrain<IProjectionsManager>(request.EventStore);
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

    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectionDefinition>> GetAllDefinitions(GetAllDefinitionsRequest request, CallContext context = default)
    {
        var projectionsManager = grainFactory.GetGrain<IProjectionsManager>(request.EventStore);
        var definitions = await projectionsManager.GetProjectionDefinitions();
        return definitions.Select(p => p.ToContract()).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Contracts.Projections.ProjectionWithDsl>> GetAllDsls(GetAllDslsRequest request, CallContext context = default)
    {
        var projectionsManager = grainFactory.GetGrain<IProjectionsManager>(request.EventStore);
        var dsls = await projectionsManager.GetProjectionDsls();
        return dsls.Select(p => new Contracts.Projections.ProjectionWithDsl
        {
            Identifier = p.Identifier,
            ReadModel = p.ReadModel,
            Dsl = p.Dsl
        }).ToArray();
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
        var projection = grainFactory.GetGrain<IProjection>(projectionKey);
        var definition = await projection.GetDefinition();
        var readModelDefinition = await storage.GetEventStore(eventStoreName).ReadModels.Get(definition.ReadModel);
        var eventTypes = await projection.GetEventTypes();
        var cursor = await eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, readModelKey, eventTypes: eventTypes);

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
        var initialState = new ExpandoObject();

        foreach (var (correlationId, events) in eventsByCorrelation)
        {
            var orderedEvents = events.OrderBy(e => e.Context.SequenceNumber).ToList();
            var firstOccurred = orderedEvents[0].Context.Occurred;

            var result = await projection.Process(namespaceName, initialState, orderedEvents);
            var jsonObject = expandoObjectConverter.ToJsonObject(result, readModelDefinition.GetSchemaForLatestGeneration());
            var readModel = JsonSerializer.Serialize(jsonObject, jsonSerializerOptions);
            initialState = result;

            snapshots.Add(new ProjectionSnapshot
            {
                ReadModel = readModel,
                Events = orderedEvents.ToContract(jsonSerializerOptions),
                Occurred = firstOccurred!,
                CorrelationId = correlationId
            });
        }

        cursor.Dispose();
        return snapshots;
    }
}
