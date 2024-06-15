// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Grains.Observation.Jobs.for_CatchUpObserver;

public class CatchUpObserverWrapper : CatchUpObserver, IGrainType
{
    public Type GrainType => typeof(ICatchUpObserver);

    public CatchUpObserverWrapper(IStorage storage) : base(storage) { }

    public Task<IImmutableList<JobStepDetails>> WrappedPrepareSteps(CatchUpObserverRequest request) => PrepareSteps(request);
    public Task<bool> WrappedCanResume() => CanResume();
    public Task WrappedOnStepCompleted(JobStepId jobStepId, JobStepResult result) => OnStepCompleted(jobStepId, result);
}
