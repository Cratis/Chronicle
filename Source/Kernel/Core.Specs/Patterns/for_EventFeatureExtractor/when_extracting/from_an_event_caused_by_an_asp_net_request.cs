// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_EventFeatureExtractor.when_extracting;

public class from_an_event_caused_by_an_asp_net_request : given.an_extractor
{
    EventFeatures _result;

    void Because() => _result = _extractor.Extract(AnEvent(causation:
    [
        new Causation(Occurred, CausationType.Root, new Dictionary<string, string>()),
        new Causation(Occurred, "ASP.NET Request", new Dictionary<string, string>
        {
            { WellKnownCausationProperties.Route, "/customers/jane@example.com" },
            { WellKnownCausationProperties.RouteTemplate, "/customers/{email}" }
        })
    ]));

    [Fact] void should_name_the_request_template() => _result.CommandType.Value.ShouldEqual("Request: /customers/{email}");
}
