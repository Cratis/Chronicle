// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs;
using Person = Cratis.Chronicle.Projections.Graphs.for_GraphDefinitionBuilder.given.ModelBound.Person;

namespace Cratis.Chronicle.Projections.Graphs.for_GraphProjections;

#pragma warning disable SA1402 // File may only contain a single type
public class when_discovering_graphs_with_views : Specification
{
    GraphProjections _graphs;

    void Establish() => _graphs = given.graph_projections.For(
        [
            typeof(Person),
            typeof(for_GraphDefinitionBuilder.given.ModelBound.Group),
            typeof(for_GraphDefinitionBuilder.given.ModelBound.Place),
            typeof(for_GraphDefinitionBuilder.given.ModelBound.MemberOf),
            typeof(for_GraphDefinitionBuilder.given.ModelBound.Visited)
        ],
        [],
        string.Empty,
        [typeof(PersonName), typeof(PersonNickname)]);

    void Because() => _graphs.Discover();

    [Fact] void should_still_register_the_graph() => _graphs.Registrations.Single().GraphName.ShouldEqual("Organization");
    [Fact] void should_read_the_valid_view() => _graphs.Views.IsView(typeof(PersonName)).ShouldBeTrue();
    [Fact] void should_report_only_the_invalid_view_as_a_failure() => _graphs.Failures.Keys.ShouldContainOnly([typeof(PersonNickname)]);
    [Fact] void should_report_why_it_was_refused() => _graphs.Failures[typeof(PersonNickname)].ShouldBeOfExactType<UnknownNodeProperty>();
}

[ViewOf<Person>]
public record PersonName(Guid Id, string Name);

[ViewOf<Person>]
public record PersonNickname(Guid Id, string Nickname);
