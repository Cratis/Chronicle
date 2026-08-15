// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_clearing_a_nested_object;

/// <summary>
/// The control for the nested clear: the badge is really there to be cleared.
/// </summary>
public class and_the_clearing_event_has_not_occurred : Specification
{
    ReadModelScenario<SecurityPass> _scenario;
    EventSourceId _id;

    void Establish()
    {
        _id = EventSourceId.New();
        _scenario = new ReadModelScenario<SecurityPass>();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_id)
            .Events(new SecurityBadgeIssued("A-114", "Reactor"));

    [Fact] void should_hold_the_badge() => _scenario.Instance!.Badge!.BadgeNumber.ShouldEqual("A-114");
}
