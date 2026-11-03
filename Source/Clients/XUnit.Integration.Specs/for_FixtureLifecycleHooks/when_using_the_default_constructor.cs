// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Cratis.Chronicle.XUnit.Integration.for_FixtureLifecycleHooks;

public class when_using_the_default_constructor : Specification
{
    DefaultFixture _fixture;

    void Because() => _fixture = new();
    async Task Destroy() => await _fixture.DisposeAsync();

    [Fact] void should_have_no_hooks() => _fixture.LifecycleHooks.ShouldBeNull();
    [Fact] void should_start_the_built_container_once() => _fixture.MongoDBContainer.Received(1).StartAsync();

    sealed class DefaultFixture : ChronicleFixture
    {
        protected override IContainer BuildContainer(INetwork network) => Substitute.For<IContainer>();
    }
}
