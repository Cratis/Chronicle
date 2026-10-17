// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.for_Projection.when_setting_definition;

public class and_the_version_does_not_require_refusal : given.a_projection_grain_with_a_versioned_definition
{
    [Theory]
    [InlineData(null, "2.0.0")]
    [InlineData("1.0.0", null)]
    [InlineData(null, null)]
    [InlineData("not-a-version", "2.0.0")]
    [InlineData("2.0.0", "2.0.0")]
    [InlineData("2.0.0+build.1", "2.0.0+build.10")]
    [InlineData("2.0.0-rc.10", "2.0.0-rc.2")]
    [InlineData("2.0.0-alpha.1", "2.0.0-alpha")]
    [InlineData("1.0.0", "1.0.0-rc.1")]
    [InlineData("2.0.0-alpha", "2.0.0-2")]
    async Task should_accept_the_definition(string? incomingVersion, string? storedVersion)
    {
        _stored = _stored with { Version = storedVersion };
        _stateStorage.State = _stored;
        var incoming = _stored with { Version = incomingVersion, Tags = ["Accepted"] };

        await _grain.SetDefinition(incoming);

        (await _grain.GetDefinition()).ShouldBeSame(incoming);
        await _stateStorage.Received(1).WriteStateAsync();
        await _comparer.Received(1).Compare(Arg.Any<ProjectionKey>(), _stored, incoming);
        _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
    }
}
