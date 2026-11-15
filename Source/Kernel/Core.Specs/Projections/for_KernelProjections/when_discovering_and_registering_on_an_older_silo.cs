// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.for_KernelProjections;

public class when_discovering_and_registering_on_an_older_silo : given.kernel_projections
{
    ProjectionDefinition[] _sharedDefinitions;
    ProjectionDefinition[] _fallbackDefinitions;

    void Establish()
    {
        _manager.RegisterShared(Arg.Any<string>(), Arg.Any<IEnumerable<ProjectionDefinition>>(), ProjectionOwner.Kernel, null, null)
            .Returns(call =>
            {
                _sharedDefinitions = call.ArgAt<IEnumerable<ProjectionDefinition>>(1).ToArray();
                return Task.FromException(new TypeLoadException("An older silo cannot resolve the shared registration method"));
            });
        _manager.Register(Arg.Any<IEnumerable<ProjectionDefinition>>(), ProjectionOwner.Kernel, null, null)
            .Returns(call =>
            {
                _fallbackDefinitions = call.ArgAt<IEnumerable<ProjectionDefinition>>(0).ToArray();
                return Task.CompletedTask;
            });
    }

    Task Because() => _kernel.DiscoverAndRegister(EventStore);

    [Fact] void should_try_shared_registration_once() => _manager.Received(1).RegisterShared(Arg.Any<string>(), Arg.Any<IEnumerable<ProjectionDefinition>>(), ProjectionOwner.Kernel, null, null);
    [Fact] void should_fall_back_once() => _manager.Received(1).Register(Arg.Any<IEnumerable<ProjectionDefinition>>(), ProjectionOwner.Kernel, null, null);
    [Fact] void should_preserve_the_definitions_on_fallback() => _fallbackDefinitions.ShouldEqual(_sharedDefinitions);
}
