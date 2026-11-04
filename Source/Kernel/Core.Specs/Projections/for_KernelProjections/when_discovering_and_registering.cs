// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Kernel;
using Cratis.Chronicle.Statistics;

namespace Cratis.Chronicle.Projections.for_KernelProjections;

public class when_discovering_and_registering : given.kernel_projections
{
    string _expectedSignature;

    void Establish() => _expectedSignature = ProjectionRegistrationSignature.For(KernelProjectionDeclarations.Lower(typeof(EventTypeStatisticsProjection)).Projections, ProjectionOwner.Kernel, null, null);

    Task Because() => _kernel.DiscoverAndRegister(EventStore);

    [Fact] void should_register_the_read_models_first() => Received.InOrder(() =>
    {
        _readModels.Register(Arg.Any<IEnumerable<ReadModelDefinition>>());
        _manager.RegisterShared(_expectedSignature, Arg.Any<IEnumerable<ProjectionDefinition>>(), ProjectionOwner.Kernel, null, null);
    });

    [Fact] void should_use_the_kernel_signature_and_owner() => _manager.Received(1).RegisterShared(_expectedSignature, Arg.Is<IEnumerable<ProjectionDefinition>>(definitions => definitions.Any() && definitions.All(definition => definition.Owner == ProjectionOwner.Kernel)), ProjectionOwner.Kernel, null, null);
    [Fact] void should_not_use_plain_registration() => _manager.DidNotReceiveWithAnyArgs().Register(default!);
}
