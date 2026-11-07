// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers;

public class when_a_reducer_registers_on_a_connection_that_was_released : given.a_registration_attempt
{
    void Establish()
    {
        _ownership.Claim("connection", AuthenticatedCalls.OwnerOf("alice"));
        _ownership.Release("connection", AuthenticatedCalls.OwnerOf("alice"));
    }

    async Task Because() => await Register(AuthenticatedCalls.As("alice"));

    [Fact] void should_refuse_the_registration() => (_error is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldBeTrue();
    [Fact] void should_not_reach_a_grain() => _grainFactory.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_resolve_the_definition() => _definitionGrain.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_use_the_mediator() => _mediator.ReceivedCalls().ShouldBeEmpty();
}
