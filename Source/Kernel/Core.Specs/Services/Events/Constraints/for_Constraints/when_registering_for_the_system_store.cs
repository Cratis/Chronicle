// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events.Constraints;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Events.Constraints.for_Constraints;

public class when_registering_for_the_system_store : Specification
{
    IGrainFactory _grainFactory;
    Exception _error;

    void Establish() => _grainFactory = Substitute.For<IGrainFactory>();

    async Task Because()
    {
        var constraints = new Constraints(_grainFactory);
        _error = await Catch.Exception(() => constraints.Register(new RegisterConstraintsRequest
        {
            EventStore = "SYSTEM"
        }));
    }

    [Fact] void should_refuse_the_registration() => (_error is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldBeTrue();
    [Fact] void should_not_reach_a_grain() => _grainFactory.ReceivedCalls().ShouldBeEmpty();
}
