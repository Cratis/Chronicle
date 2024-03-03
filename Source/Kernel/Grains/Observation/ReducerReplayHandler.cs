// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Observation;

namespace Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="ICanHandleReplayForObserver"/> for reducers.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReducerReplayHandler"/> class.
/// </remarks>
/// <param name="kernel"><see cref="IKernel"/> for accessing global artifacts.</param>
public class ReducerReplayHandler(IKernel kernel) : ICanHandleReplayForObserver
{
    /// <inheritdoc/>
    public Task<bool> CanHandle(ObserverDetails observerDetails) => Task.FromResult(observerDetails.Type == ObserverType.Reducer);

    /// <inheritdoc/>
    public async Task BeginReplayFor(ObserverDetails observerDetails)
    {
        var eventStore = kernel.GetEventStore((string)observerDetails.Key.MicroserviceId);
        var @namespace = eventStore.GetNamespace(observerDetails.Key.TenantId);

        if (await eventStore.ReducerPipelineDefinitions.HasFor(observerDetails.Identifier))
        {
            var definition = await eventStore.ReducerPipelineDefinitions.GetFor(observerDetails.Identifier);
            var pipeline = await @namespace.ReducerPipelines.GetFor(definition);
            await pipeline.BeginReplay();
        }
    }

    /// <inheritdoc/>
    public async Task EndReplayFor(ObserverDetails observerDetails)
    {
        var eventStore = kernel.GetEventStore((string)observerDetails.Key.MicroserviceId);
        var @namespace = eventStore.GetNamespace(observerDetails.Key.TenantId);

        if (await eventStore.ReducerPipelineDefinitions.HasFor(observerDetails.Identifier))
        {
            var definition = await eventStore.ReducerPipelineDefinitions.GetFor(observerDetails.Identifier);
            var pipeline = await @namespace.ReducerPipelines.GetFor(definition);
            await pipeline.EndReplay();
        }
    }
}
