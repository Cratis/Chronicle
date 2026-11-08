// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.Graphs.for_GraphProjectionBuilder;

public class when_defining_property_mappings : Specification
{
    [EventType]
    public record ScoreChanged(Guid TeamId, int Points, int Penalty, string Title);

    public record Team(Guid TeamId, string Title, int Score, int Penalty, int Changes, string Kind);

    [Graph("Teams")]
    sealed class TeamGraph : IGraphProjection
    {
        public void Define(IGraphProjectionBuilder builder) => builder
            .Node<Team>(n => n
                .IdentifiedBy(t => t.TeamId)
                .From<ScoreChanged>(f => f
                    .UsingKey(e => e.TeamId)
                    .OnlyWhenExisting()
                    .Add(t => t.Score).ToEventProperty(e => e.Points)
                    .Subtract(t => t.Penalty).ToEventProperty(e => e.Penalty)
                    .Count(t => t.Changes)
                    .Set(t => t.Kind).ToValue("league")
                    .Set(t => t.Title).ToEventProperty(e => e.Title)));
    }

    GraphModel _model;

    void Because() => _model = new GraphProjectionBuilder(new CamelCaseNamingPolicy()).Build(new TeamGraph());

    [Fact] void should_name_the_graph_after_the_attribute() => _model.Name.ShouldEqual("Teams");
    [Fact] void should_use_the_label_of_the_node_type() => _model.Nodes[0].Label.ShouldEqual("Team");
    [Fact] void should_use_the_declared_key() => _model.Nodes[0].KeyProperty.ShouldEqual("teamId");
    [Fact] void should_key_by_the_event_property() => _model.Nodes[0].From[0].Key.ShouldEqual("teamId");
    [Fact] void should_only_update_an_existing_node() => _model.Nodes[0].From[0].UpdateOnly.ShouldBeTrue();
    [Fact] void should_add_an_event_property() => _model.Nodes[0].From[0].Properties["score"].ShouldEqual("$add(points)");
    [Fact] void should_subtract_an_event_property() => _model.Nodes[0].From[0].Properties["penalty"].ShouldEqual("$subtract(penalty)");
    [Fact] void should_count() => _model.Nodes[0].From[0].Properties["changes"].ShouldEqual("$count");
    [Fact] void should_set_a_value() => _model.Nodes[0].From[0].Properties["kind"].ShouldEqual("$value(league)");
    [Fact] void should_set_an_event_property() => _model.Nodes[0].From[0].Properties["title"].ShouldEqual("title");
}
