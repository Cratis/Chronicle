// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Jobs;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Storage;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs.for_CatchUpObserver;

public class CatchUpObserverWrapper : CatchUpObserver, IGrainType
{
    public Type GrainType => typeof(ICatchUpObserver);

    public CatchUpObserverWrapper(IStorage storage) : base(storage) { }

    public Task<IImmutableList<JobStepDetails>> WrappedPrepareSteps(CatchUpObserverRequest request) => PrepareSteps(request);
    public Task<bool> WrappedCanResume() => CanResume();
    public Task WrappedOnStepCompleted(JobStepId jobStepId, JobStepResult result) => OnStepCompleted(jobStepId, result);
}
