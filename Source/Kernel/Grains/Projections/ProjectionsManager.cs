// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Namespaces;
using Cratis.Chronicle.Projections.Definitions;
using Orleans.BroadcastChannel;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionsManager"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionsManager"/> class.
/// </remarks>
public class ProjectionsManager : Grain, IProjectionsManager, IOnBroadcastChannelSubscribed
{
    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var eventStoreName = this.GetPrimaryKeyString();
        var namespaces = await GrainFactory.GetGrain<INamespaces>(eventStoreName).GetAll();
        foreach (var namespaceName in namespaces)
        {
        }
    }

    /// <inheritdoc/>
    public async Task Register(IEnumerable<ProjectionDefinition> definitions)
    {
        var eventStoreName = this.GetPrimaryKeyString();
        var namespaceNames = await GrainFactory.GetGrain<INamespaces>(eventStoreName).GetAll();

        foreach (var registration in definitions)
        {
            // var projectionDefinition = registration.Projection;
            // var pipelineDefinition = registration.Pipeline;

            // var (isNew, hasChanged) = await eventStoreInstance.ProjectionDefinitions.IsNewOrChanged(projectionDefinition);
            // if (isNew)
            // {
            //     _logger.ProjectionIsNew(projectionDefinition.Identifier, projectionDefinition.Name);
            // }

            // if (hasChanged || isNew)
            // {
            //     await RegisterProjectionAndPipeline(
            //         projectionDefinition,
            //         pipelineDefinition);
            // }

            // if (hasChanged)
            // {
            //     _logger.ProjectionHasChanged(projectionDefinition.Identifier, projectionDefinition.Name);
            //     await AddReplayRecommendationForAllNamespaces(projectionDefinition.Identifier, namespaceNames);
            // }

            foreach (var @namespace in namespaceNames)
            {
            }
        }
    }

    /// <inheritdoc/>
    public Task OnSubscribed(IBroadcastChannelSubscription streamSubscription)
    {
        streamSubscription.Attach<NamespaceAdded>(OnNamespaceAdded, OnError);
        return Task.CompletedTask;
    }

    async Task OnNamespaceAdded(NamespaceAdded added)
    {
        await Task.CompletedTask;
    }

    Task OnError(Exception exception)
    {
        return Task.CompletedTask;
    }
}
