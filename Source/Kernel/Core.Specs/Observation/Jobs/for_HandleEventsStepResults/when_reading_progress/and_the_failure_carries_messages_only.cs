// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsStepResults.when_reading_progress;

public class and_the_failure_carries_messages_only : Specification
{
    readonly JsonSerializerOptions _options = new();
    HandleEventsForPartitionResult _progress;
    Exception _error;

    void Because() => _error = Catch.Exception(() => _progress = HandleEventsStepResults.ReadProgress(JobStepResult.Failed("failed"), _options));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_record_no_progress() => _progress.ShouldBeNull();
}
