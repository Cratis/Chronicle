// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.Projections.Graphs.for_GraphProjections;

#pragma warning disable SA1402 // File may only contain a single type
public class when_discovering_a_graph_whose_elements_name_different_sinks : Specification
{
    GraphProjections _graphs;

    void Establish() => _graphs = given.graph_projections.For([typeof(Employee), typeof(Department)], [], string.Empty);

    void Because() => _graphs.Discover();

    [Fact] void should_not_register_the_graph() => _graphs.Registrations.ShouldBeEmpty();
    [Fact] void should_report_the_conflict() => _graphs.Failures.Values.Single().ShouldBeOfExactType<GraphHasConflictingSinks>();
}

[Graph("Company")]
[Node]
[Sink("people")]
[FromEvent<for_GraphDefinitionBuilder.given.PersonRegistered>]
public record Employee([Key] Guid PersonId, string Name);

[Graph("Company")]
[Node]
[Sink("organization")]
[FromEvent<for_GraphDefinitionBuilder.given.GroupCreated>]
public record Department([Key] Guid GroupId, string Name);
