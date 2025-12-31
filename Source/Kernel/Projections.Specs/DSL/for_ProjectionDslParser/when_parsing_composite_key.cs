// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.DSL;

namespace Cratis.Chronicle.Projections.for_ProjectionDslParser;

public class when_parsing_composite_key : Specification
{
    const string Dsl = @"Users
| key=userId:UserRegistered.userId, tenantId:UserRegistered.tenantId
| name=UserRegistered.name";

    ProjectionDefinition _result;

    void Because() => _result = Cratis.Chronicle.Projections.DSL.ProjectionDsl.Parse(
        Dsl,
        new ProjectionId("test-projection"),
        ProjectionOwner.Client,
        EventSequenceId.Log);

    [Fact] void should_set_composite_key_expression() => _result.From.First().Value.Key.Value.ShouldContain("$composite(");
    [Fact] void should_include_userId_in_composite_key() => _result.From.First().Value.Key.Value.ShouldContain("userId=userId");
    [Fact] void should_include_tenantId_in_composite_key() => _result.From.First().Value.Key.Value.ShouldContain("tenantId=tenantId");
}
