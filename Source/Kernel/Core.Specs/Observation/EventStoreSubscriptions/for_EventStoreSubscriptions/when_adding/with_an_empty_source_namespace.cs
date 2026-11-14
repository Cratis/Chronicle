// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_adding;

public class with_an_empty_source_namespace : given.a_service_that_can_add
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _service.Add(RequestWith(" ")));

    [Fact] void should_refuse_with_invalid_argument() => ((RpcException)_error).StatusCode.ShouldEqual(StatusCode.InvalidArgument);
    [Fact] void should_not_append_an_event() => _eventSequence.ReceivedCalls().ShouldBeEmpty();
}
