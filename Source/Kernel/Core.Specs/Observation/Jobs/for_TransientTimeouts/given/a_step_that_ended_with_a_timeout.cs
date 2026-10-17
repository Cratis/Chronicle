// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_TransientTimeouts.given;

public class a_step_that_ended_with_a_timeout : Specification
{
    protected JsonSerializerOptions _options;
    protected Key _partition;
    protected JobStepResult _timedOut;
    protected JobStepResult _otherFailure;
    protected JobStepResult _success;

    void Establish()
    {
        _options = new JsonSerializerOptions();
        _partition = new Key("some-partition", Properties.ArrayIndexers.NoIndexers);
        _timedOut = JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
            new HandleEventsForPartitionResult(EventSequenceNumber.Unavailable)
            {
                TransientFailure = new(_partition, 42ul, ["Response did not arrive on time"])
            },
            ["Response did not arrive on time"],
            string.Empty));
        _otherFailure = JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
            new HandleEventsForPartitionResult(EventSequenceNumber.Unavailable),
            ["Something went wrong"],
            string.Empty));
        _success = JobStepResult.Succeeded(new HandleEventsForPartitionResult(100ul));
    }
}
