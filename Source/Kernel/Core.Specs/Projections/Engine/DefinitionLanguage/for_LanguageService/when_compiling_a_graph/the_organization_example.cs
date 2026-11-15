// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Graphs;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling_a_graph;

/// <summary>
/// Pins the definition the declaration lowers to against the definition the .NET client builds for the equivalent
/// model-bound graph (for_GraphDefinitionBuilder.when_building.a_model_bound_graph).
/// </summary>
public class the_organization_example : given.an_organization_graph_declaration
{
    CompiledGraph _compiled;
    GraphProjectionDefinition _graph;
    GraphDefinition _readModelGraph;
    CompilerErrors _errors;
    JsonObject _schema;

    void Because()
    {
        _compiled = CompileGraph(Declaration, out _errors);
        _graph = _compiled.Projection.Graph!;
        _readModelGraph = GraphDefinition.FromSchema(_compiled.ReadModel.GetSchemaForLatestGeneration());
        _schema = JsonNode.Parse(_compiled.ReadModel.GetSchemaForLatestGeneration().ToJson())!.AsObject();
    }

    [Fact] void should_be_recognized_as_a_graph_declaration() => _languageService.IsGraphDeclaration(Declaration).ShouldBeTrue();
    [Fact] void should_have_no_errors() => _errors.ShouldBeNull();
    [Fact] void should_name_the_read_model_after_the_graph() => _compiled.ReadModel.Identifier.ShouldEqual(new ReadModelIdentifier("Organization"));
    [Fact] void should_make_the_projection_observe_the_graph() => _compiled.Projection.ReadModel.ShouldEqual(new ReadModelIdentifier("Organization"));
    [Fact] void should_name_the_projection() => _compiled.Projection.Identifier.ShouldEqual(new ProjectionId("OrganizationGraph"));
    [Fact] void should_use_the_neo4j_sink() => _compiled.ReadModel.Sink.Type.ShouldEqual(Concepts.Sinks.WellKnownSinkTypes.Neo4j);
    [Fact] void should_mark_the_schema_as_a_graph() => _schema["x-graph"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_describe_the_nodes_and_edges() => _schema["properties"]!.AsObject().Select(_ => _.Key).Order().ShouldContainOnly("Group", "MEMBER_OF", "Person", "Place", "VISITED");
    [Fact] void should_describe_the_key_of_a_node() => _schema["properties"]!["Person"]!["x-graph-key"]!.GetValue<string>().ShouldEqual("personId");
    [Fact] void should_describe_the_end_nodes_of_an_edge() => _schema["properties"]!["MEMBER_OF"]!["x-graph-source"]!.GetValue<string>().ShouldEqual("Person");
    [Fact] void should_describe_the_identity_of_an_edge() => _schema["properties"]!["VISITED"]!["x-graph-identity"]!.GetValue<string>().ShouldEqual("visitId");
    [Fact] void should_read_back_as_a_graph_definition() => _readModelGraph.Nodes.Select(_ => _.Label).Order().ShouldContainOnly("Group", "Person", "Place");
    [Fact] void should_resolve_a_concept_to_its_primitive() => _schema["properties"]!["Person"]!["properties"]!["personId"]!["format"]!.GetValue<string>().ShouldEqual("guid");
    [Fact] void should_have_the_declared_properties_on_an_edge() => _schema["properties"]!["MEMBER_OF"]!["properties"]!.AsObject().Select(_ => _.Key).ShouldContainOnly("since");
    [Fact] void should_key_a_node_by_the_event_source_id() => _graph.Nodes["Person"].From["PersonRegistered+1"].Key.Value.ShouldEqual("$eventSourceId");
    [Fact] void should_map_properties_by_name() => _graph.Nodes["Person"].From["PersonRegistered+1"].Properties["name"].ShouldEqual("name");
    [Fact] void should_not_map_the_key_as_a_property() => _graph.Nodes["Person"].From["PersonRegistered+1"].Properties.ContainsKey("personId").ShouldBeFalse();
    [Fact] void should_detach_when_asked_to() => _graph.Nodes["Person"].RemovedWith["PersonRemoved+1"].Detach.ShouldBeTrue();
    [Fact] void should_key_a_node_by_an_event_property() => _graph.Nodes["Group"].From["MemberAdded+1"].Key.Value.ShouldEqual("groupId");
    [Fact] void should_increment_a_counter() => _graph.Nodes["Group"].From["MemberAdded+1"].Properties["memberCount"].ShouldEqual("$increment");
    [Fact] void should_decrement_a_counter() => _graph.Nodes["Group"].From["MemberRemoved+1"].Properties["memberCount"].ShouldEqual("$decrement");
    [Fact] void should_resolve_the_keys_of_an_edge() => _graph.Edges["MEMBER_OF"].From["MemberAdded+1"].SourceKey.Value.ShouldEqual("personId");
    [Fact] void should_map_an_edge_property_from_the_event_context() => _graph.Edges["MEMBER_OF"].From["MemberAdded+1"].Properties["since"].ShouldEqual("$eventContext(occurred)");
    [Fact] void should_remove_an_edge() => _graph.Edges["MEMBER_OF"].RemovedWith["MemberRemoved+1"].TargetKey.Value.ShouldEqual("groupId");
    [Fact] void should_take_the_identity_of_an_edge_from_the_event() => _graph.Edges["VISITED"].From["PlaceVisited+1"].Identity!.Value.ShouldEqual("visitId");
    [Fact] void should_count_on_an_edge() => _graph.Edges["VISITED"].From["PlaceVisited+1"].Properties["times"].ShouldEqual("$count");
    [Fact] void should_not_map_the_identity_of_an_edge_as_a_property() => _graph.Edges["VISITED"].From["PlaceVisited+1"].Properties.ContainsKey("visitId").ShouldBeFalse();
    [Fact] void should_have_no_removal_on_a_node_without_one() => _graph.Nodes["Place"].RemovedWith.ShouldBeEmpty();
    [Fact] void should_compile_through_the_projection_entry_point() => _languageService.Compile(Declaration, ProjectionOwner.Client, [], _eventTypeSchemas).Match(_ => true, _ => false).ShouldBeTrue();
    [Fact] void should_resolve_the_read_model_identifier() => _languageService.GetReadModelIdentifier(Declaration).Match(_ => _.Value, _ => string.Empty).ShouldEqual("Organization");
}
