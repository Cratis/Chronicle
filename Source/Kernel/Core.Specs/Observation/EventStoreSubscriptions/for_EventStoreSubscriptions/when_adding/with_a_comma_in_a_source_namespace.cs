// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_adding;

public class with_a_comma_in_a_source_namespace : given.a_service_that_can_add
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _service.Add(RequestWith("Default", "raalabs,raalabs")));

    [Fact] void should_refuse_the_request() => _error.ShouldBeOfExactType<RpcException>();
    [Fact] void should_refuse_with_invalid_argument() => ((RpcException)_error).StatusCode.ShouldEqual(StatusCode.InvalidArgument);
    [Fact] void should_say_why() => ((RpcException)_error).Status.Detail.ShouldContain("comma");
    [Fact] void should_not_reserve_a_definition() => _subscriptionsManager.DidNotReceiveWithAnyArgs().Add(default!);
    [Fact] void should_not_append_an_event() => _eventSequence.ReceivedCalls().ShouldBeEmpty();
}
