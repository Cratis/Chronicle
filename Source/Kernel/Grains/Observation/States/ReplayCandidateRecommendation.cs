// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Cratis.Chronicle.Grains.Recommendations;
using Cratis.Jobs;

namespace Cratis.Chronicle.Grains.Observation.States;

/// <summary>
/// Represents an implementation of <see cref="IReplayCandidateRecommendation"/>.
/// </summary>
public class ReplayCandidateRecommendation : Recommendation<ReplayCandidateRequest>, IReplayCandidateRecommendation
{
    /// <inheritdoc/>
    protected override async Task OnPerform(ReplayCandidateRequest request)
    {
        this.GetPrimaryKey(out var keyAsString);
        var key = (RecommendationKey)keyAsString;
        var jobsManager = GrainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(key.EventStore, key.Namespace));

        var observer = GrainFactory.GetGrain<IObserver>(request.ObserverId, keyExtension: request.ObserverKey);
        var subscription = await observer.GetSubscription();
        var eventTypes = await observer.GetEventTypes();

        await jobsManager.Start<IReplayObserver, ReplayObserverRequest>(
            JobId.New(),
            new ReplayObserverRequest(
                request.ObserverId,
                request.ObserverKey,
                subscription,
                eventTypes));
    }
}
