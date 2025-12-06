// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Grains;
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
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for accessing services.</param>
/// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
internal sealed class Projections(
    IClusterClient clusterClient,
    IGrainFactory grainFactory,
    IServiceProvider serviceProvider,
    JsonSerializerOptions jsonSerializerOptions) : IProjections
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
    public async Task<ProjectionResult> GetInstanceById(GetInstanceByIdRequest request, CallContext context = default)
    {
        var projectionKey = new ImmediateProjectionKey(
            request.ProjectionId,
            request.EventStore,
            request.Namespace,
            request.EventSequenceId,
            request.ReadModelKey);

        var projection = grainFactory.GetGrain<Grains.Projections.IImmediateProjection>(projectionKey);

        var result = await projection.GetModelInstance();
        return result.ToContract(jsonSerializerOptions);
    }

    /// <inheritdoc/>
    public async Task<ProjectionResult> GetInstanceByIdForSession(GetInstanceByIdForSessionRequest request, CallContext context = default)
    {
        var projectionKey = new ImmediateProjectionKey(
            request.ProjectionId,
            request.EventStore,
            request.Namespace,
            request.EventSequenceId,
            request.ReadModelKey,
            request.SessionId);

        var projection = grainFactory.GetGrain<Grains.Projections.IImmediateProjection>(projectionKey);

        var result = await projection.GetModelInstance();
        return result.ToContract(jsonSerializerOptions);
    }

    /// <inheritdoc/>
    public async Task<ProjectionResult> GetInstanceByIdForSessionWithEventsApplied(GetInstanceByIdForSessionWithEventsAppliedRequest request, CallContext context = default)
    {
        var projectionKey = new ImmediateProjectionKey(
            request.ProjectionId,
            request.EventStore,
            request.Namespace,
            request.EventSequenceId,
            request.ReadModelKey,
            request.SessionId);

        var projection = grainFactory.GetGrain<Grains.Projections.IImmediateProjection>(projectionKey);

        var eventsToApply = request.Events.Select(_ => _.ToChronicle()).ToArray();
        var result = await projection.GetCurrentModelInstanceWithAdditionalEventsApplied(eventsToApply);
        return result.ToContract(jsonSerializerOptions);
    }

    /// <inheritdoc/>
    public IObservable<ProjectionChangeset> Watch(ProjectionWatchRequest request, CallContext context = default)
    {
        var streamProvider = clusterClient.GetStreamProvider(WellKnownStreamProviders.ProjectionChangesets);
        var streamId = StreamId.Create(new StreamIdentity(Guid.Empty, request.ProjectionId));

        var stream = streamProvider.GetStream<Grains.Projections.ProjectionChangeset>(streamId);
        StreamSubscriptionHandle<Grains.Projections.ProjectionChangeset>? subscription = null;

        var observable = Observable.Create<ProjectionChangeset>(async (observer, cancellationToken) =>
        {
            subscription = await stream.SubscribeAsync((changeset, _) =>
            {
                observer.OnNext(new ProjectionChangeset
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

        var projection = grainFactory.GetGrain<Grains.Projections.IImmediateProjection>(projectionKey);
        await projection.Dehydrate();
    }

    /// <inheritdoc/>
    public async Task<GetSnapshotsByIdResponse> GetSnapshotsById(GetSnapshotsByIdRequest request, CallContext context = default)
    {
        var projectionGrain = grainFactory.GetGrain<Grains.Projections.IProjection>(
            new ProjectionKey(request.ProjectionId, request.EventStore));
        
        var definition = await projectionGrain.GetDefinition();
        var snapshots = await GetSnapshotsForProjection(
            request.EventStore,
            request.Namespace,
            request.EventSequenceId,
            definition);

        return new GetSnapshotsByIdResponse
        {
            Snapshots = snapshots.ToList()
        };
    }

    async Task<IEnumerable<ProjectionSnapshot>> GetSnapshotsForProjection(
        string eventStoreName,
        string namespaceName,
        string eventSequenceId,
        Concepts.Projections.Definitions.ProjectionDefinition definition)
    {
        var storage = serviceProvider.GetRequiredService<IStorage>();
        var eventSequenceStorage = storage
            .GetEventStore(eventStoreName)
            .GetNamespace(namespaceName)
            .GetEventSequence(eventSequenceId);

        // For now, we'll get all events - in the future we could filter by projection event types
        var tailSequenceNumber = await eventSequenceStorage.GetTailSequenceNumber();

        if (tailSequenceNumber == Concepts.Events.EventSequenceNumber.Unavailable)
        {
            return [];
        }

        var cursor = await eventSequenceStorage.GetFromSequenceNumber(
            Concepts.Events.EventSequenceNumber.First);

        var eventsByCorrelation = new Dictionary<Guid, List<Concepts.Events.AppendedEvent>>();

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

            // For now, we'll return empty read model as we can't easily replay without full projection engine setup
            snapshots.Add(new ProjectionSnapshot
            {
                ReadModel = "{}",
                Events = eventsJson,
                Occurred = firstOccurred,
                CorrelationId = correlationId
            });
        }

        cursor.Dispose();
        return snapshots;
    }
}
