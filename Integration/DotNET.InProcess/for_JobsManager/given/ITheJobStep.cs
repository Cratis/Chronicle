// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;
using Orleans.Concurrency;

namespace Cratis.Chronicle.InProcess.Integration.for_JobsManager.given;

internal interface ITheJobStep : IJobStep<TheJobStepRequest, TheJobStepResult, TheJobStepState>
{
    [AlwaysInterleave]
    public Task SetCompleted();
    [AlwaysInterleave]
    public Task SetFailed();
    [AlwaysInterleave]
    public Task IncrementStopped();
    [AlwaysInterleave]
    public Task IncrementPerformCalled();
}
