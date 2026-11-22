// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleClientFixture;

public class when_inherited_fixture_variants_disable_bootstrap : Specification
{
    IChronicleFixture _chronicle;
    ChronicleClientFixture<IChronicleFixture>[] _fixtures;

    void Establish()
    {
        _chronicle = Substitute.For<IChronicleFixture>();
        _fixtures = [new orleans_fixture(_chronicle), new specification_fixture(_chronicle), new custom_factory_specification(_chronicle)];
    }

    async Task Because()
    {
        foreach (var fixture in _fixtures)
        {
            await ((IAsyncLifetime)fixture).InitializeAsync();
            await ((IAsyncLifetime)fixture).DisposeAsync();
        }
    }

    [Fact] void should_not_bootstrap_or_reset_or_clean_up_any_backing_fixture() => _chronicle.ReceivedCalls().ShouldBeEmpty();

    sealed class orleans_fixture(IChronicleFixture fixture) : ChronicleOrleansFixture<IChronicleFixture>(fixture)
    {
        protected override bool ShouldBootstrap => false;
    }

    sealed class specification_fixture(IChronicleFixture fixture) : Specification<IChronicleFixture>(fixture)
    {
        protected override bool ShouldBootstrap => false;
    }

    sealed class custom_factory_specification(IChronicleFixture fixture) : Specification<IChronicleFixture, ChronicleWebApplicationFactory<object>, object>(fixture)
    {
        protected override bool ShouldBootstrap => false;
    }
}
