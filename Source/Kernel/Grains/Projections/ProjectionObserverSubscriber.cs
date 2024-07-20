// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Definitions;
using Cratis.Chronicle.Projections.Pipelines;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionObserverSubscriber"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionObserverSubscriber"/> class.
/// </remarks>
/// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating projections.</param>
/// <param name="projectionPipelineFactory"><see cref="IProjectionPipelineFactory"/> for creating projection pipelines.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Projections)]
public class ProjectionObserverSubscriber(
    IProjectionFactory projectionFactory,
    IProjectionPipelineFactory projectionPipelineFactory) : Grain<ProjectionDefinition>, IProjectionObserverSubscriber, INotifyProjectionDefinitionsChanged
{
    ObserverSubscriberKey _key = new(ObserverId.Unspecified, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, EventSequenceId.Unspecified, EventSourceId.Unspecified, string.Empty);
    IProjectionPipeline? _pipeline;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = ObserverSubscriberKey.Parse(this.GetPrimaryKeyString());
        var projection = GrainFactory.GetGrain<IProjection>(new ProjectionKey(_key.ObserverId, _key.EventStore, _key.Namespace, _key.EventSequenceId));
        await projection.SubscribeDefinitionsChanged(this.AsReference<INotifyProjectionDefinitionsChanged>());
        await HandlePipeline();
    }

    /// <inheritdoc/>
    public void OnProjectionDefinitionsChanged()
    {
        ReadStateAsync().Wait();
        HandlePipeline().Wait();
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        if (_pipeline is null)
        {
            return ObserverSubscriberResult.Disconnected(EventSequenceNumber.Unavailable);
        }

        AppendedEvent? lastSuccessfullyObservedEvent = default;

        try
        {
            foreach (var @event in events)
            {
                await _pipeline.Handle(@event);
                lastSuccessfullyObservedEvent = @event;
            }
            return ObserverSubscriberResult.Ok(events.Last().Metadata.SequenceNumber);
        }
        catch (Exception ex)
        {
            return new(
                ObserverSubscriberState.Failed,
                lastSuccessfullyObservedEvent?.Metadata.SequenceNumber ?? EventSequenceNumber.Unavailable,
                ex.GetAllMessages(),
                ex.StackTrace ?? string.Empty);
        }
    }

    async Task HandlePipeline()
    {
        var projection = await projectionFactory.CreateFrom(_key.EventStore, _key.Namespace, State);
        _pipeline = projectionPipelineFactory.CreateFrom(projection, State);
    }
}
