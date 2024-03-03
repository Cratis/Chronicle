// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Types;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserverService"/>.
/// </summary>
[Reentrant]
public class ObserverService : GrainService, IObserverService
{
    readonly IInstancesOf<ICanHandleReplayForObserver> _replayHandlers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverService"/> class.
    /// </summary>
    /// <param name="grainId">The <see cref="GrainId"/> for the service.</param>
    /// <param name="silo">The <see cref="Silo"/> the service belongs to.</param>
    /// <param name="replayHandlers">All instances of <see cref="ICanHandleReplayForObserver"/>.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public ObserverService(
        GrainId grainId,
        Silo silo,
        IInstancesOf<ICanHandleReplayForObserver> replayHandlers,
        ILoggerFactory loggerFactory)
        : base(grainId, silo, loggerFactory)
    {
        _replayHandlers = replayHandlers;
    }

    /// <inheritdoc/>
    public override Task Init(IServiceProvider serviceProvider) =>
        base.Init(serviceProvider);

    /// <inheritdoc/>
    public async Task BeginReplayFor(ObserverDetails observerDetails)
    {
        foreach (var handler in _replayHandlers)
        {
            if (await handler.CanHandle(observerDetails))
            {
                await handler.BeginReplayFor(observerDetails);
            }
        }
    }

    /// <inheritdoc/>
    public async Task EndReplayFor(ObserverDetails observerDetails)
    {
        foreach (var handler in _replayHandlers)
        {
            if (await handler.CanHandle(observerDetails))
            {
                await handler.EndReplayFor(observerDetails);
            }
        }
    }
}
