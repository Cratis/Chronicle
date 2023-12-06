// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Kernel.Grains.Jobs;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IConsolidateStateForObservers"/>.
/// </summary>
public class ConsolidateStateForObservers : Job<ConsolidateStateForObserveRequest, JobState<ConsolidateStateForObserveRequest>>, IConsolidateStateForObservers
{
    /// <inheritdoc/>
    protected override bool RemoveAfterCompleted => true;

    /// <inheritdoc/>
    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(ConsolidateStateForObserveRequest request) =>
        Task.FromResult<IImmutableList<JobStepDetails>>(request.Observers.Select(CreateStep<IConsolidateStateForObserver>).ToImmutableList());
}
