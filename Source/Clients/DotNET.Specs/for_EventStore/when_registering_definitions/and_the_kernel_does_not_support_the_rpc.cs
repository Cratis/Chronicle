// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;
using Grpc.Core;

namespace Cratis.Chronicle.for_EventStore.when_registering_definitions;

public class and_the_kernel_does_not_support_the_rpc : given.an_event_store_with_buildable_definitions
{
    RpcException _unimplemented;

    void Establish()
    {
        _unimplemented = new RpcException(new Status(StatusCode.Unimplemented, "RegisterDeployment is unavailable"));
        _services.EventStores.RegisterDeployment(Arg.Any<Contracts.EventStores.RegisterDeploymentRequest>()).Returns(Task.FromException<Contracts.Commands.CommandResult>(_unimplemented));
    }

    async Task Because() => _failure = await Catch.Exception(() => _eventStore.RegisterDefinitions(new DeploymentGeneration(1)));

    [Fact]
    void should_propagate_the_rpc_failure() => _failure.ShouldBeSame(_unimplemented);

    [Fact]
    void should_not_send_artifacts() => _eventStore.EventTypes.DidNotReceive().Register();
}
