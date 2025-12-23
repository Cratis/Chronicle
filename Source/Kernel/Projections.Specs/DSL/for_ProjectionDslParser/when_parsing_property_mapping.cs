// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.DSL;

namespace Cratis.Chronicle.Projections.for_ProjectionDslParser;

public class when_parsing_property_mapping : Specification
{
    const string Dsl = @"Users
| name=UserRegistered.name";

    ProjectionDefinition _result;

    void Because() => _result = ProjectionDsl.Parse(
        Dsl,
        new ProjectionId("test-projection"),
        ProjectionOwner.Client,
        EventSequenceId.Log);

    [Fact] void should_have_one_event_type_in_from() => _result.From.Count.ShouldEqual(1);
    [Fact] void should_map_name_property() => _result.From.First().Value.Properties.ContainsKey(new("name")).ShouldBeTrue();
}
