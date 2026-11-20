// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;
using NSubstitute;

namespace Cratis.Chronicle.Integration.for_ReusableBackingContainer;

public class and_disposed_twice : Cratis.Specifications.Specification
{
    IContainer _container;
    ReusableBackingContainer _owner;

    void Establish()
    {
        _container = Substitute.For<IContainer>();
        _owner = new(() => _container, _ => Task.CompletedTask, _ => { });
    }

    async Task Because()
    {
        await _owner.GetOrStart();
        await _owner.DisposeAsync();
        await _owner.DisposeAsync();
    }

    [Fact] void should_dispose_the_container_once() => _container.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IContainer.DisposeAsync)).ShouldEqual(1);
}
