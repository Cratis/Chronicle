// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Generations;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Recovery of the observers of cataloged later generations.
/// </summary>
public partial class ProjectionsManager
{
    async Task SubscribeCatalogedGenerations(EventStoreNamespaceName @namespace, IEnumerable<ProjectionDefinition> definitions)
    {
        if (generations is null)
        {
            return;
        }

        foreach (var definition in definitions)
        {
            try
            {
                foreach (var execution in await generations.ResolveCataloged(_eventStoreName, @namespace, definition.Identifier))
                {
                    await SubscribeGeneration(@namespace, execution);
                }
            }
            catch (Exception exception)
            {
                logger.FailedSubscribingCatalogedGeneration(exception, definition.Identifier, @namespace);
            }
        }
    }

    async Task<bool> CollidesWithGeneration(ProjectionDefinition definition, EventStoreNamespaceName @namespace)
    {
        if (generations is null || !await generations.IsGenerationObserver(_eventStoreName, @namespace, definition.EventSequenceId, definition.Identifier))
        {
            return false;
        }

        logger.RefusedGenerationObserverCollision(definition.Identifier, @namespace);
        return true;
    }

    async Task SubscribeGeneration(EventStoreNamespaceName @namespace, GenerationExecution execution)
    {
        var definition = execution.Inputs.Projection;
        var projection = execution.Projection;
        var observer = GrainFactory.GetGrain<IObserver>(new ObserverKey(execution.Binding.Identity.ObserverId, _eventStoreName, @namespace, execution.Binding.EventSequence));

        // Same three-branch selection as the legacy subscribe, duplicated on purpose so the legacy path stays untouched.
        Task SubscribeAs<TSubscriber>()
            where TSubscriber : IObserverSubscriber
        {
            if (definition.SubscribesToAllEvents)
            {
                return observer.SubscribeToAllEvents<TSubscriber>(ObserverType.Projection, localSiloDetails.SiloAddress);
            }

            if (definition.GenerationDelivery == EventGenerationDelivery.Pinned)
            {
                return observer.SubscribeWithGenerationDelivery<TSubscriber>(ObserverType.Projection, projection.EventTypes, localSiloDetails.SiloAddress, definition.GenerationDelivery);
            }

            return observer.Subscribe<TSubscriber>(ObserverType.Projection, projection.EventTypes, localSiloDetails.SiloAddress);
        }

        await _subscriptionThrottle.WaitAsync();
        try
        {
            await (projection.IsEventSourceKeyed
                ? SubscribeAs<IProjectionObserverSubscriber>()
                : SubscribeAs<ICollapsingProjectionObserverSubscriber>());
        }
        finally
        {
            _subscriptionThrottle.Release();
        }
    }
}
