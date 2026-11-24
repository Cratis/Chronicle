// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.Integration.Specifications.for_JobsManager.given;

[GenerateSerializer]
[Alias("Cratis.Chronicle.Integration.Specifications.for_JobsManager.given.JobWithSingleStepRequest")]
public record JobWithSingleStepRequest([Id(0)] bool KeepAfterCompleted = false, [Id(1)] bool ShouldFail = false, [Id(2)] TimeSpan? WaitTime = null, [Id(3)] int WaitCount = 0) : IJobRequest;
