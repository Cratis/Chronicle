// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Grains;
using Orleans.Streams;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjections"/>.
/// </summary>
/// <param name="clusterClient"><see cref="IClusterClient"/> for interacting with the cluster.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
public class Projections(IClusterClient clusterClient, IGrainFactory grainFactory) : IProjections
{
    /// <inheritdoc/>
    public Task Register(RegisterRequest request, CallContext context = default)
    {
        var projectionsManager = grainFactory.GetGrain<Grains.Projections.IProjectionsManager>(request.EventStore);
        var projections = request.Projections.Select(_ => _.ToChronicle()).ToArray();

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
            request.ModelKey);

        var projection = grainFactory.GetGrain<Grains.Projections.IImmediateProjection>(projectionKey);

        var result = await projection.GetModelInstance();
        return result.ToContract();
    }

    /// <inheritdoc/>
    public async Task<ProjectionResult> GetInstanceByIdForSession(GetInstanceByIdForSessionRequest request, CallContext context = default)
    {
        var projectionKey = new ImmediateProjectionKey(
            request.ProjectionId,
            request.EventStore,
            request.Namespace,
            request.EventSequenceId,
            request.ModelKey,
            request.SessionId);

        var projection = grainFactory.GetGrain<Grains.Projections.IImmediateProjection>(projectionKey);

        var result = await projection.GetModelInstance();
        return result.ToContract();
    }

    /// <inheritdoc/>
    public async Task<ProjectionResult> GetInstanceByIdForSessionWithEventsApplied(GetInstanceByIdForSessionWithEventsAppliedRequest request, CallContext context = default)
    {
        var projectionKey = new ImmediateProjectionKey(
            request.ProjectionId,
            request.EventStore,
            request.Namespace,
            request.EventSequenceId,
            request.ModelKey,
            request.SessionId);

        var projection = grainFactory.GetGrain<Grains.Projections.IImmediateProjection>(projectionKey);

        var eventsToApply = request.Events.Select(_ => _.ToChronicle()).ToArray();
        var result = await projection.GetCurrentModelInstanceWithAdditionalEventsApplied(eventsToApply);
        return result.ToContract();
    }

    /// <inheritdoc/>
    public IObservable<ProjectionChangeset> Watch(ObserveChangesRequest request, CallContext context = default)
    {
        var streamProvider = clusterClient.GetStreamProvider(WellKnownStreamProviders.Projections);
        var streamId = StreamId.Create(new StreamIdentity(Guid.Empty, request.ProjectionId));

        var stream = streamProvider.GetStream<ProjectionChangeset>(streamId);

        var observable = Observable.Create<ProjectionChangeset>(async (observer, cancellationToken) =>
        {
            var subscription = await stream.SubscribeAsync((changeset, _) =>
            {
                Console.WriteLine("Hello world");
                observer.OnNext(changeset);

                return Task.CompletedTask;
            });
        });

        return new Subject<ProjectionChangeset>();
    }

    /// <inheritdoc/>
    public async Task DehydrateSession(DehydrateSessionRequest request, CallContext context = default)
    {
        var projectionKey = new ImmediateProjectionKey(
            request.ProjectionId,
            request.EventStore,
            request.Namespace,
            request.EventSequenceId,
            request.ModelKey,
            request.SessionId);

        var projection = grainFactory.GetGrain<Grains.Projections.IImmediateProjection>(projectionKey);
        await projection.Dehydrate();
    }
}
