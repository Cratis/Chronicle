// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Integration.Specifications.for_JobsManager.given;

public class TheJobStepState : JobStepState
{
    public bool ShouldFail { get; set; }
    public TimeSpan WaitTime { get; set; } = TimeSpan.Zero;
    public int WaitCount { get; set; }
    public bool Completed { get; set; }
    public bool Failed { get; set; }
    public int NumTimesStopped { get; set; }
    public int NumTimesPerformCalled { get; set; }
}
