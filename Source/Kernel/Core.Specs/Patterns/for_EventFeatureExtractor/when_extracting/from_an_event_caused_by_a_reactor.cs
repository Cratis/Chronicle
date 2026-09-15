// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_EventFeatureExtractor.when_extracting;

public class from_an_event_caused_by_a_reactor : given.an_extractor
{
    EventFeatures _result;

    void Because() => _result = _extractor.Extract(AnEvent(causation:
    [
        new Causation(Occurred, CausationType.Root, new Dictionary<string, string>()),
        new Causation(Occurred, "Client Reactor", new Dictionary<string, string>
        {
            { WellKnownCausationProperties.ReactorId, "MyApp.Reactors.ExpenseReportSubmitted" }
        })
    ]));

    [Fact] void should_name_the_reactor() => _result.CommandType.Value.ShouldEqual("Reactor: MyApp.Reactors.ExpenseReportSubmitted");
}
