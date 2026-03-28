// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;

namespace Cratis.Chronicle.Integration.Specifications.for_JobsManager.given;

public record JobWithSingleStepRequest(bool KeepAfterCompleted = false, bool ShouldFail = false, TimeSpan? WaitTime = null, int WaitCount = 0) : IJobRequest;
