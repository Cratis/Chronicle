// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_EventFeatureExtractor.when_extracting;

/// <summary>
/// Every command shares one causation type, so a link that names the command it stands for is read by that name.
/// Reading the type instead would file every command in the store under a single value and there would be no
/// behavior left to mine.
/// </summary>
public class from_an_event_caused_by_named_commands : given.an_extractor
{
    EventFeatures _result;

    void Because() => _result = _extractor.Extract(AnEvent(causation:
    [
        new Causation(Occurred, CausationType.Root, new Dictionary<string, string>()),
        new Causation(Occurred, "ASP.NET Request", new Dictionary<string, string>()),
        new Causation(Occurred, "Command", new Dictionary<string, string>
        {
            { WellKnownCausationProperties.CommandType, "SubmitExpenseReport" }
        }),
        new Causation(Occurred, "Command", new Dictionary<string, string>
        {
            { WellKnownCausationProperties.CommandType, "ApproveExpenseReport" }
        })
    ]));

    [Fact] void should_name_the_command_that_produced_the_event() => _result.CommandType.Value.ShouldEqual("ApproveExpenseReport");
    [Fact] void should_name_the_command_one_level_up() => _result.CausedByCommand.Value.ShouldEqual("SubmitExpenseReport");
}
