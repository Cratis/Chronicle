// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_EventFeatureExtractor.when_extracting;

/// <summary>
/// In an event-sourced store the fact that was recorded is itself the action, so an event with nothing named above
/// it still has an action worth mining - its own type.
/// </summary>
public class from_an_event_nothing_above_it_named : given.an_extractor
{
    EventFeatures _result;

    void Because() => _result = _extractor.Extract(AnEvent(causation:
    [
        new Causation(Occurred, CausationType.Root, new Dictionary<string, string>()),
        new Causation(Occurred, CausationType.Unknown, new Dictionary<string, string>())
    ]));

    [Fact] void should_fall_back_to_the_event_type() => _result.CommandType.Value.ShouldEqual("ExpenseReportApproved");
    [Fact] void should_not_invent_a_command_a_level_up() => _result.CausedByCommand.ShouldEqual(FacetValue.Unspecified);
}
