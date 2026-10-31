// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.for_Projection.when_setting_definition;

public class and_the_stored_version_is_newer : given.a_projection_grain_with_a_versioned_definition
{
    [Theory]
    [InlineData("1.0.0", "2.0.0")]
    [InlineData("2.0.0-rc.2", "2.0.0-rc.10")]
    [InlineData("2.0.0-alpha", "2.0.0-alpha.1")]
    [InlineData("1.0.0-rc.1", "1.0.0")]
    [InlineData("2.0.0-2", "2.0.0-alpha")]
    async Task should_refuse_the_definition_without_side_effects(string incomingVersion, string storedVersion)
    {
        _stored = _stored with { Version = storedVersion };
        _stateStorage.State = _stored;
        await _grain.SetDefinition(_stored with { Version = incomingVersion, Tags = ["Stale"] });
        (await _grain.GetDefinition()).ShouldBeSame(_stored);
        await _stateStorage.DidNotReceive().WriteStateAsync();
        await _comparer.DidNotReceive().Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>());
        _storage.DidNotReceive().GetEventStore(Arg.Any<EventStoreName>());
        _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
    }
}
