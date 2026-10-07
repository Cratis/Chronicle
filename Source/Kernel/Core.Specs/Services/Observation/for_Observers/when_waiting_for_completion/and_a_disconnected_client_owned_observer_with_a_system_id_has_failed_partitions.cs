// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_a_disconnected_client_owned_observer_with_a_system_id_has_failed_partitions : given.a_client_owned_observer_with_a_system_id
{
    WaitForObserverCompletionResponse _result;

    void Establish() => _failedPartitionsStorage.GetFor(Arg.Any<IEnumerable<ObserverId>>()).Returns(new Concepts.Observation.FailedPartitions
    {
        Partitions = [new() { ObserverId = SystemObserverId, Partition = Concepts.Keys.Key.Undefined }]
    });

    async Task Because() => _result = await _observers.WaitForCompletion(_request);

    [Fact] void should_not_report_success() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_client_owned_observers_failed_partition() => _result.FailedPartitions.Select(_ => _.ObserverId).ShouldContainOnly(SystemObserverId);
    [Fact] void should_not_time_out() => _result.TimedOut.ShouldBeFalse();
    [Fact] void should_leave_no_observers_outstanding() => _result.OutstandingObservers.ShouldBeEmpty();
}
