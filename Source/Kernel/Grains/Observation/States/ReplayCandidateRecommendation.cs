// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Cratis.Chronicle.Grains.Recommendations;

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
        var jobsManager = GrainFactory.GetJobsManager(key.EventStore, key.Namespace);

        var observer = GrainFactory.GetGrain<IObserver>(request.ObserverKey);
        var subscription = await observer.GetSubscription();
        var eventTypes = await observer.GetEventTypes();

        await jobsManager.Start<IReplayObserver, ReplayObserverRequest>(new(request.ObserverKey, request.ObserverType, subscription, eventTypes));
    }
}
