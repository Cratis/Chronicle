// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_EventFeatureExtractor.when_extracting;

public class from_an_event_with_a_causation_chain : given.an_extractor
{
    EventFeatures _result;

    void Because() => _result = _extractor.Extract(AnEvent(causation:
    [
        new Causation(Occurred, CausationType.Root, new Dictionary<string, string>()),
        new Causation(Occurred, "SubmitExpenseReport", new Dictionary<string, string>()),
        new Causation(Occurred, "ApproveExpenseReport", new Dictionary<string, string>())
    ]));

    [Fact] void should_take_the_command_type_from_the_most_recent_named_link() => _result.CommandType.Value.ShouldEqual("ApproveExpenseReport");
    [Fact] void should_take_the_caused_by_command_from_one_level_up() => _result.CausedByCommand.Value.ShouldEqual("SubmitExpenseReport");
}
