// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Cratis.Chronicle.XUnit.Integration.for_FixtureLifecycleHooks;

public class when_constructing_with_hooks : Specification
{
    FixtureLifecycleHooks _hooks;
    HookedFixture _fixture;
    readonly List<string> _observations = [];
    IContainer? _built;
    IContainer? _started;

    void Establish() => _hooks = new()
    {
        BeforeBuild = () => _observations.Add("build"),
        ContainerBuilt = container =>
        {
            _built = container;
            _observations.Add("built");
        },
        Start = (container, _) =>
        {
            _started = container;
            _observations.Add("start");
            return Task.CompletedTask;
        }
    };

    void Because() => _fixture = new(_hooks);

    async Task Destroy() => await _fixture.DisposeAsync();

    [Fact] void should_assign_hooks_before_virtual_startup() => _fixture.SeenDuringBuild.ShouldEqual(_hooks);
    [Fact] void should_observe_build_before_start() => _observations.ShouldEqual(["build", "built", "start"]);
    [Fact] void should_pass_the_actual_built_container_to_start() => _started.ShouldEqual(_built);

    sealed class HookedFixture(FixtureLifecycleHooks hooks) : ChronicleFixture(hooks)
    {
        internal FixtureLifecycleHooks? SeenDuringBuild { get; private set; }

        protected override IContainer BuildContainer(INetwork network)
        {
            SeenDuringBuild = LifecycleHooks;
            return Substitute.For<IContainer>();
        }
    }
}
