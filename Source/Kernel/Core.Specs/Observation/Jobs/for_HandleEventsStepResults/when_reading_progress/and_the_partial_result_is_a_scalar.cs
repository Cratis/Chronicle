// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsStepResults.when_reading_progress;

public class and_the_partial_result_is_a_scalar : Specification
{
    readonly JsonSerializerOptions _options = new();
    HandleEventsForPartitionResult _progress;
    Exception _error;

    void Because()
    {
        using var document = JsonDocument.Parse("42");
        _error = Catch.Exception(() => _progress = HandleEventsStepResults.ReadProgress(
            JobStepResult.Failed(new PerformJobStepError(document.RootElement, ["failed"], string.Empty, false)), _options));
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_record_no_progress() => _progress.ShouldBeNull();
}
