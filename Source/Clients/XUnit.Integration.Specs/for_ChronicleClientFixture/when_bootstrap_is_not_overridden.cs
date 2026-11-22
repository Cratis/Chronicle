// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.XUnit.Integration.for_ChronicleClientFixture.given;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleClientFixture;

public class when_bootstrap_is_not_overridden : Specification
{
    IChronicleFixture _chronicle;
    bootstrap_fixture _fixture;

    void Establish()
    {
        _chronicle = Substitute.For<IChronicleFixture>();
        _chronicle.RemoveAllDatabases(Arg.Any<IEnumerable<string>?>()).Returns(Task.CompletedTask);
        _fixture = new(_chronicle);
    }

    async Task Because()
    {
        await _fixture.InitializeAsync();
        await _fixture.DisposeAsync();
    }

    [Fact] void should_run_bootstrap_reset_scenario_and_teardown_in_order() => _fixture.Steps.ShouldEqual(new[] { "bootstrap-and-reset", "scenario", "teardown" });
    [Fact] void should_clean_up_storage() => _chronicle.Received(1).RemoveAllDatabases(Arg.Any<IEnumerable<string>?>());
}
