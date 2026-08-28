// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_EventFeatureExtractor.when_extracting;

public class from_an_event_caused_by_a_single_named_command : given.an_extractor
{
    EventFeatures _result;

    void Because() => _result = _extractor.Extract(AnEvent(causation:
    [
        new Causation(Occurred, CausationType.Root, new Dictionary<string, string>()),
        new Causation(Occurred, "ASP.NET Request", new Dictionary<string, string>()),
        new Causation(Occurred, "Command", new Dictionary<string, string>
        {
            { WellKnownCausationProperties.CommandType, "ApproveExpenseReport" }
        })
    ]));

    [Fact] void should_name_the_command() => _result.CommandType.Value.ShouldEqual("ApproveExpenseReport");
    [Fact] void should_read_the_request_as_the_link_above_it() => _result.CausedByCommand.Value.ShouldEqual("ASP.NET Request");
}
