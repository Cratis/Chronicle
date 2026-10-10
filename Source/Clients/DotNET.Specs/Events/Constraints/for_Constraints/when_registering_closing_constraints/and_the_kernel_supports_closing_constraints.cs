// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_Constraints.when_registering_closing_constraints;

public class and_the_kernel_supports_closing_constraints : given.a_closing_constraint
{
    void Establish()
    {
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor, IKernelCapabilities>();
        ((IKernelCapabilities)connection).Capabilities.Returns([KernelCapabilities.ClosesStreamConstraints]);
        ((IChronicleServicesAccessor)connection).Services.Returns(_services);
        _eventStore.Connection.Returns(connection);
        _constraints = new(_eventStore, _constraintsProviders);
        _constraints.Discover();
    }

    Task Because() => _constraints.Register();

    [Fact] void should_register_the_closing_definition() => _constraintsService.Received(1).Register(Arg.Is<RegisterConstraintsRequest>(request => request.Constraints.Single().Type == Contracts.Events.Constraints.ConstraintType.ClosesStream));
}
