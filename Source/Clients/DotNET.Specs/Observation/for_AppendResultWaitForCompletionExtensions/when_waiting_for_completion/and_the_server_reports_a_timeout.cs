// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_AppendResultWaitForCompletionExtensions.when_waiting_for_completion;

public class and_the_server_reports_a_timeout : given.an_append_result_for_completion
{
    void Establish() => _observers.WaitForCompletion(
        Arg.Any<Contracts.Observation.WaitForObserverCompletionRequest>(),
        Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(new Contracts.Observation.WaitForObserverCompletionResponse
    {
        TimedOut = true,
        OutstandingObservers = ["observer-a"]
    });

    async Task Because() => _result = await _appendResult.WaitForCompletion();

    [Fact] void should_report_a_timeout() => _result.TimedOut.ShouldBeTrue();
    [Fact] void should_name_the_outstanding_observer() => _result.OutstandingObservers.ShouldContain("observer-a");
    [Fact] void should_not_report_an_observer_failure() => _result.FailedPartitions.ShouldBeEmpty();
}
