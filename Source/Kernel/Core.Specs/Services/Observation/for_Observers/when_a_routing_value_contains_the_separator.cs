// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Security.given;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Observation.for_Observers;

public class when_a_routing_value_contains_the_separator : given.all_dependencies
{
    RpcException _error;

    async Task Because() => _error = (RpcException)await Catch.Exception(() => _observers.Replay(
        new Replay { EventStore = "application", Namespace = "default#System", ObserverId = "observer", EventSequenceId = "event-log" },
        CallFor(new Probe(), CallerNamed(a_system_store_caller.Administrator))));

    [Fact] void should_be_an_invalid_argument() => _error.StatusCode.ShouldEqual(StatusCode.InvalidArgument);
    [Fact] void should_carry_the_separator_message() => _error.Status.Detail.ShouldEqual(SystemStoreAccess.SeparatorRefused);
    [Fact] void should_not_reach_a_grain() => _grainFactory.ReceivedCalls().ShouldBeEmpty();
}
