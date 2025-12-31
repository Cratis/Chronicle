// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.DSL;

namespace Cratis.Chronicle.Projections.for_ProjectionDslParser;

public class when_parsing_increment_operation : Specification
{
    const string Dsl = @"Users
| currentOrderCount increment by OrderStarted";

    ProjectionDefinition _result;

    void Because() => _result = Cratis.Chronicle.Projections.DSL.ProjectionDsl.Parse(
        Dsl,
        new ProjectionId("test-projection"),
        ProjectionOwner.Client,
        EventSequenceId.Log);

    [Fact] void should_have_one_event_type_in_from() => _result.From.Count.ShouldEqual(1);
    [Fact] void should_set_increment_expression() => _result.From.First().Value.Properties[new("currentOrderCount")].ShouldEqual("$increment");
}
