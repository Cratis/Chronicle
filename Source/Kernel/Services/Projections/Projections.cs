// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Grains;
using Cratis.Chronicle.Services.Projections.Definitions;
using Orleans.Streams;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjections"/>.
/// </summary>
/// <param name="clusterClient"><see cref="IClusterClient"/> for interacting with the cluster.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
internal sealed class Projections(
    IClusterClient clusterClient,
    IGrainFactory grainFactory,
    JsonSerializerOptions jsonSerializerOptions) : IProjections
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
            request.ReadModelKey);

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
            request.ReadModelKey,
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
            request.ReadModelKey,
            request.SessionId);

        var projection = grainFactory.GetGrain<Grains.Projections.IImmediateProjection>(projectionKey);

        var eventsToApply = request.Events.Select(_ => _.ToChronicle()).ToArray();
        var result = await projection.GetCurrentModelInstanceWithAdditionalEventsApplied(eventsToApply);
        return result.ToContract();
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
}
