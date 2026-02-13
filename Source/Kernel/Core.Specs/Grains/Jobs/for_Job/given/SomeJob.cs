// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Grains.Jobs.for_Job.given;

public class SomeJob : Job<SomeRequest, SomeJobState>
{
    public List<JobStepDetails> StepsToPrepare = [];
    public bool OnCompletedThrows;
    public bool ShouldBeRemovedAfterCompleted;
    public bool ShouldBeResumable;

    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(SomeRequest request) =>
        Task.FromResult<IImmutableList<JobStepDetails>>(StepsToPrepare.ToImmutableList());

    protected override bool KeepAfterCompleted => !ShouldBeRemovedAfterCompleted;
    protected override Task OnCompleted() => OnCompletedThrows
        ? Task.FromException(new Exception())
        : Task.CompletedTask;

    protected override Task<bool> CanResume() => Task.FromResult(ShouldBeResumable);
}