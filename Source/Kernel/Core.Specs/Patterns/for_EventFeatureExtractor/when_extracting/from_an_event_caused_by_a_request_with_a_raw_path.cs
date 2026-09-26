// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_EventFeatureExtractor.when_extracting;

public class from_an_event_caused_by_a_request_with_a_raw_path : given.an_extractor
{
    EventFeatures _result;

    void Because() => _result = _extractor.Extract(AnEvent(causation:
    [
        new Causation(Occurred, "ASP.NET Request", new Dictionary<string, string>
        {
            { WellKnownCausationProperties.Route, "/customers/jane@example.com" }
        })
    ]));

    [Fact] void should_not_name_the_raw_path() => _result.CommandType.Value.ShouldEqual("ASP.NET Request");
}
