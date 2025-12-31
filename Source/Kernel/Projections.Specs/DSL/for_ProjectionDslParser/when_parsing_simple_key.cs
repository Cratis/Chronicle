// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.DSL;

namespace Cratis.Chronicle.Projections.for_ProjectionDslParser;

public class when_parsing_simple_key : Specification
{
    const string Dsl = @"Users
| key=UserRegistered.userId
| name=UserRegistered.name";

    ProjectionDefinition _result;

    void Because() => _result = Cratis.Chronicle.Projections.DSL.ProjectionDsl.Parse(
        Dsl,
        new ProjectionId("test-projection"),
        ProjectionOwner.Client,
        EventSequenceId.Log);

    [Fact] void should_set_key_expression() => _result.From.First().Value.Key.Value.ShouldEqual("userId");
}
