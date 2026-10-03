// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Patterns;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_unsubscribed_registered_pattern_capture_has_failed_partitions : given.a_registered_pattern_capture
{
    WaitForObserverCompletionResponse _result;

    void Establish() => _namespaceFailures.GetFor(Arg.Any<IEnumerable<ObserverId>>()).Returns(new Concepts.Observation.FailedPartitions
    {
        Partitions = [new() { ObserverId = PatternCapture.ObserverIdentifier, Partition = Concepts.Keys.Key.Undefined }]
    });

    async Task Because() => _result = await _completion.WaitForCompletion(_request);

    [Fact] void should_complete_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_include_pattern_capture_failures() => _result.FailedPartitions.ShouldBeEmpty();
    [Fact] void should_leave_no_observers_outstanding() => _result.OutstandingObservers.ShouldBeEmpty();
}
