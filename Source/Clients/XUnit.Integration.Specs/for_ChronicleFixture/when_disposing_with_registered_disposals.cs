// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleFixture;

public class when_disposing_with_registered_disposals : Specification
{
    readonly List<string> _order = [];
    ChronicleFixtureForSpecs _fixture;

    void Establish()
    {
        _fixture = new(new FixtureLifecycleHooks
        {
            Start = (_, _) => Task.CompletedTask,
            Dispose = (_, dispose) =>
            {
                _order.Add("container");
                return dispose();
            }
        });
        _fixture.RegisterDisposal(() =>
        {
            _order.Add("first");
            return ValueTask.CompletedTask;
        });
        _fixture.RegisterDisposal(() =>
        {
            _order.Add("second");
            return ValueTask.CompletedTask;
        });
    }

    async Task Because() => await _fixture.DisposeAsync();

    [Fact] void should_run_the_registered_disposals_in_reverse_order_before_the_container_goes() => _order.ShouldEqual(["second", "first", "container"]);

    sealed class ChronicleFixtureForSpecs(FixtureLifecycleHooks hooks) : ChronicleFixture(hooks)
    {
        protected override IContainer BuildContainer(INetwork network) => Substitute.For<IContainer>();
    }
}
