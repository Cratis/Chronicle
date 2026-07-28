// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Jobs;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.given;

public class a_debouncing_job_step : a_performing_job_step
{
    protected const int CheckpointInterval = 3;

    protected TestStorageStats _stateStorageStats => _silo.StorageManager.GetStorageStats(nameof(JobStepState))!;

    protected override Cratis.Chronicle.Configuration.Jobs CreateJobsConfig() => new() { StepCheckpointBatchInterval = CheckpointInterval };

    void Establish() => _stateStorageStats.ResetCounts();
}
