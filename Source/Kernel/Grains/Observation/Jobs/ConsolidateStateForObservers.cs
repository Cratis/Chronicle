// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IConsolidateStateForObservers"/>.
/// </summary>
public class ConsolidateStateForObservers : Job<ConsolidateStateForObserveRequest, JobState>, IConsolidateStateForObservers
{
    /// <inheritdoc/>
    protected override bool RemoveAfterCompleted => true;

    /// <inheritdoc/>
    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(ConsolidateStateForObserveRequest request) =>
        Task.FromResult<IImmutableList<JobStepDetails>>(request.Observers.Select(CreateStep<IConsolidateStateForObserver>).ToImmutableList());
}
