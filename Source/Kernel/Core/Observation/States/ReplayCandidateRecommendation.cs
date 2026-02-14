// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Recommendations;

namespace Cratis.Chronicle.Observation.States;

/// <summary>
/// Represents an implementation of <see cref="IReplayCandidateRecommendation"/>.
/// </summary>
public class ReplayCandidateRecommendation : Recommendation<ReplayCandidateRequest>, IReplayCandidateRecommendation
{
    /// <inheritdoc/>
    protected override async Task OnPerform(ReplayCandidateRequest request)
    {
        var observer = GrainFactory.GetGrain<IObserver>(request.ObserverKey);
        await observer.Replay();
    }
}
