// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Jobs;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Grains.Observation.Jobs;
using Aksio.Cratis.Kernel.Grains.Recommendations;

namespace Aksio.Cratis.Kernel.Grains.Observation.States;

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
        var jobsManager = GrainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(key.MicroserviceId, key.TenantId));

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
