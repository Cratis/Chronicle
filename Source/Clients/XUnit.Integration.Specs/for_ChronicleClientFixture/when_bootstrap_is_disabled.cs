// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.XUnit.Integration.for_ChronicleClientFixture.given;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleClientFixture;

public class when_bootstrap_is_disabled : Specification
{
    IChronicleFixture _chronicle;
    disabled_fixture _fixture;

    void Establish()
    {
        _chronicle = Substitute.For<IChronicleFixture>();
        _fixture = new(_chronicle);
    }

    async Task Because()
    {
        await _fixture.InitializeAsync();
        await _fixture.DisposeAsync();
        DisposeSynchronously();
    }

    void DisposeSynchronously() => _fixture.Dispose();

    [Fact] void should_skip_bootstrap_reset_scenario_and_teardown_hooks() => _fixture.Steps.ShouldBeEmpty();
    [Fact] void should_not_touch_backing_storage_or_containers() => _chronicle.ReceivedCalls().ShouldBeEmpty();

    sealed class disabled_fixture(IChronicleFixture fixture) : bootstrap_fixture(fixture)
    {
        protected override bool ShouldBootstrap => false;
    }
}
