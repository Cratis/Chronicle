// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Server.for_MultiplexedObservation;

public class when_one_stream_fails : given.a_multiplexed_host
{
    given.OpenStream<global::Cratis.Chronicle.Contracts.Observation.Reactors.ReactorMessage, global::Cratis.Chronicle.Contracts.Observation.EventsToObserve> _failing;
    given.OpenStream<global::Cratis.Chronicle.Contracts.Observation.Reactors.ReactorMessage, global::Cratis.Chronicle.Contracts.Observation.EventsToObserve> _healthy;

    async Task Because()
    {
        _failing = OpenReactor("failing");
        _healthy = OpenReactor("healthy");
        await WaitForReactors(2);
        var failingIndex = ReactorIdOf(_reactors.Calls[0].Received[0]) == "failing" ? 0 : 1;
        _reactors.Calls[failingIndex].Fail(new RpcException(new(StatusCode.PermissionDenied, "Not yours")));
        await WaitUntil(() => _failing.Error is not null);
        _reactors.Calls[1 - failingIndex].Emit(Events("still-served"));
        await WaitUntil(() => _healthy.Received.Count == 1);
    }

    void Destroy()
    {
        _failing.Dispose();
        _healthy.Dispose();
    }

    [Fact] void should_fail_the_stream_with_the_status_it_failed_with() => ((RpcException)_failing.Error!).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_carry_the_detail() => ((RpcException)_failing.Error!).Status.Detail.ShouldEqual("Not yours");
    [Fact] void should_keep_serving_the_other_stream() => _healthy.Received.Single().Partition.ShouldEqual("still-served");
    [Fact] void should_not_fail_the_other_stream() => _healthy.Error.ShouldBeNull();
    [Fact] void should_still_use_one_call_to_the_kernel() => _service.Calls.ShouldEqual(1);
}
