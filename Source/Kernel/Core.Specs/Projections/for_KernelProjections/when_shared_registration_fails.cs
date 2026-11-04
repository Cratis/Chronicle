// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.for_KernelProjections;

public class when_shared_registration_fails : given.kernel_projections
{
    readonly InvalidOperationException _failure = new("Registration failed");
    Exception? _caught;

    void Establish() => _manager.RegisterShared(Arg.Any<string>(), Arg.Any<IEnumerable<ProjectionDefinition>>(), ProjectionOwner.Kernel, null, null).Returns(Task.FromException(_failure));

    async Task Because()
    {
        try
        {
            await _kernel.DiscoverAndRegister(EventStore);
        }
        catch (Exception exception)
        {
            _caught = exception;
        }
    }

    [Fact] void should_propagate_the_failure() => _caught.ShouldEqual(_failure);
    [Fact] void should_not_retry_as_plain_registration() => _manager.DidNotReceiveWithAnyArgs().Register(default!);
}
