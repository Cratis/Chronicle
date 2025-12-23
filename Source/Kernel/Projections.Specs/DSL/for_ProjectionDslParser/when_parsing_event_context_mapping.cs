// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.DSL;

namespace Cratis.Chronicle.Projections.for_ProjectionDslParser;

public class when_parsing_event_context_mapping : Specification
{
    const string Dsl = @"Users
| occurred=$eventContext.occurred
| eventSourceId=$eventContext.eventSourceId";

    ProjectionDefinition _result;

    void Because() => _result = ProjectionDsl.Parse(
        Dsl,
        new ProjectionId("test-projection"),
        ProjectionOwner.Client,
        EventSequenceId.Log);

    [Fact] void should_have_occurred_in_from_every() => _result.FromEvery.Properties.ContainsKey(new("occurred")).ShouldBeTrue();
    [Fact] void should_have_event_source_id_in_from_every() => _result.FromEvery.Properties.ContainsKey(new("eventSourceId")).ShouldBeTrue();
    [Fact] void should_set_occurred_expression() => _result.FromEvery.Properties[new("occurred")].ShouldEqual("$eventContext.occurred");
}
