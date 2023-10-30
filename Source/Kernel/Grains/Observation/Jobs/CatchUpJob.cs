// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Grains.Observation.States;
using Aksio.Cratis.Kernel.Keys;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Represents a job for catching up an observer.
/// </summary>
public class CatchUpJob : Job<CatchUpRequest, JobState<CatchUpRequest>>, ICatchUpJob
{
    readonly IObserverKeyIndexes _observerKeyIndexes;
    CatchUpRequest? _request;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayJob"/> class.
    /// </summary>
    /// <param name="observerKeyIndexes"><see cref="IObserverKeyIndexes"/> for getting index for observer to replay.</param>
    public CatchUpJob(IObserverKeyIndexes observerKeyIndexes)
    {
        _observerKeyIndexes = observerKeyIndexes;
    }

    /// <inheritdoc/>
    public override async Task OnCompleted()
    {
        if (_request == null) return;
        var observer = GrainFactory.GetGrain<IObserver>(_request.ObserverId, _request.ObserverKey);
        await observer.TransitionTo<Routing>();
    }

    /// <inheritdoc/>
    protected override async Task PrepareSteps(CatchUpRequest request)
    {
        _request = request;
        var index = await _observerKeyIndexes.GetFor(
            request.ObserverId,
            request.ObserverKey);

        var keys = await index.GetKeys(request.FromEventSequenceNumber);
        await foreach (var key in keys)
        {
            await AddStep<IHandleEventsForPartition, HandleEventsForPartitionArguments>(
                new HandleEventsForPartitionArguments(
                    request.ObserverId,
                    request.ObserverKey,
                    request.ObserverSubscription,
                    key,
                    request.FromEventSequenceNumber,
                    request.EventTypes));

            break;
        }

        await WriteStateAsync();
    }
}
