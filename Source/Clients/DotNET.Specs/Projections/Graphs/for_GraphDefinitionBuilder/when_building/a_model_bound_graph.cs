// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.Graphs.for_GraphDefinitionBuilder.when_building;

public class a_model_bound_graph : given.a_graph_definition_builder
{
    GraphRegistration _registration;
    GraphProjectionDefinition _graph;
    JsonObject _schema;

    void Because()
    {
        _registration = _builder.Build(ModelBoundModel(), "organization", "Neo4j");
        _graph = _registration.Projection.Graph!;
        _schema = JsonNode.Parse(_registration.ReadModel.Schema)!.AsObject();
    }

    [Fact] void should_name_the_read_model_after_the_graph() => _registration.ReadModel.Type.Identifier.ShouldEqual("Organization");
    [Fact] void should_use_the_given_sink_type() => _registration.ReadModel.Sink.TypeId.ShouldEqual("Neo4j");
    [Fact] void should_make_the_projection_observe_the_graph() => _registration.Projection.ReadModel.ShouldEqual("Organization");
    [Fact] void should_mark_the_schema_as_a_graph() => _schema["x-graph"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_describe_the_nodes_and_edges() => _schema["properties"]!.AsObject().Select(_ => _.Key).OrderBy(_ => _).ShouldContainOnly("Group", "MEMBER_OF", "Person", "Place", "VISITED");
    [Fact] void should_describe_the_key_of_a_node() => _schema["properties"]!["Person"]!["x-graph-key"]!.GetValue<string>().ShouldEqual("personId");
    [Fact] void should_describe_the_end_nodes_of_an_edge() => _schema["properties"]!["MEMBER_OF"]!["x-graph-source"]!.GetValue<string>().ShouldEqual("Person");
    [Fact] void should_describe_the_identity_of_an_edge() => _schema["properties"]!["VISITED"]!["x-graph-identity"]!.GetValue<string>().ShouldEqual("visitId");
    [Fact] void should_not_have_the_end_node_keys_as_properties_of_an_edge() => _schema["properties"]!["MEMBER_OF"]!["properties"]!.AsObject().Select(_ => _.Key).ShouldContainOnly("since");
    [Fact] void should_key_a_node_by_the_event_source_id_by_default() => _graph.Nodes["Person"].From["PersonRegistered+1"].Key.ShouldEqual("$eventSourceId");
    [Fact] void should_map_properties_by_name() => _graph.Nodes["Person"].From["PersonRegistered+1"].Properties["name"].ShouldEqual("name");
    [Fact] void should_not_map_the_key_as_a_property() => _graph.Nodes["Person"].From["PersonRegistered+1"].Properties.ContainsKey("personId").ShouldBeFalse();
    [Fact] void should_detach_when_asked_to() => _graph.Nodes["Person"].RemovedWith["PersonRemoved+1"].Detach.ShouldBeTrue();
    [Fact] void should_key_a_node_by_an_event_property() => _graph.Nodes["Group"].From["MemberAdded+1"].Key.ShouldEqual("groupId");
    [Fact] void should_increment_a_counter() => _graph.Nodes["Group"].From["MemberAdded+1"].Properties["memberCount"].ShouldEqual("$increment");
    [Fact] void should_decrement_a_counter() => _graph.Nodes["Group"].From["MemberRemoved+1"].Properties["memberCount"].ShouldEqual("$decrement");
    [Fact] void should_resolve_the_keys_of_an_edge() => _graph.Edges["MEMBER_OF"].From["MemberAdded+1"].SourceKey.ShouldEqual("personId");
    [Fact] void should_map_an_edge_property_from_the_event_context() => _graph.Edges["MEMBER_OF"].From["MemberAdded+1"].Properties["since"].ShouldEqual("$eventContext(occurred)");
    [Fact] void should_remove_an_edge() => _graph.Edges["MEMBER_OF"].RemovedWith["MemberRemoved+1"].TargetKey.ShouldEqual("groupId");
    [Fact] void should_take_the_identity_of_an_edge_from_the_event() => _graph.Edges["VISITED"].From["PlaceVisited+1"].Identity.ShouldEqual("visitId");
    [Fact] void should_count_on_an_edge() => _graph.Edges["VISITED"].From["PlaceVisited+1"].Properties["times"].ShouldEqual("$count");
}
