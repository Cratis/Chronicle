// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.Projections.Graphs;
using Cratis.Chronicle.Properties;
using static Cratis.Chronicle.Storage.MongoDB.Projections.Definitions.ProjectionDefinitionConverters;

using KernelEventType = Cratis.Chronicle.Concepts.Events.EventType;
using KernelProjectionDefinition = Cratis.Chronicle.Concepts.Projections.Definitions.ProjectionDefinition;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionDefinitionConverters;

/// <summary>
/// The graph mappings of a projection must survive storage, otherwise the projection would stop projecting after a restart.
/// </summary>
public class when_round_tripping_a_graph_projection : Specification
{
    KernelProjectionDefinition _restored;

    void Because()
    {
        var definition = new KernelProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "graph",
            "organization",
            true,
            false,
            new(),
            new Dictionary<KernelEventType, FromDefinition>(),
            new Dictionary<KernelEventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<KernelEventType, RemovedWithDefinition>(),
            new Dictionary<KernelEventType, RemovedWithJoinDefinition>())
        {
            Graph = new(
                new Dictionary<string, GraphNodeProjectionDefinition>
                {
                    ["Person"] = new(
                        new Dictionary<string, GraphNodeFromDefinition> { ["PersonRegistered+1"] = new("personId", new Dictionary<string, string> { ["name"] = "name" }, true) },
                        new Dictionary<string, GraphNodeRemovedWithDefinition> { ["PersonRemoved+1"] = new("personId", true) })
                },
                new Dictionary<string, GraphEdgeProjectionDefinition>
                {
                    ["MEMBER_OF"] = new(
                        new Dictionary<string, GraphEdgeFromDefinition> { ["MemberAdded+1"] = new("personId", "groupId", "membershipId", new Dictionary<string, string> { ["since"] = "$eventContext(occurred)" }, new PropertyExpression("slots")) },
                        new Dictionary<string, GraphEdgeRemovedWithDefinition> { ["MemberRemoved+1"] = new("personId", "groupId", null, new PropertyExpression("slots")) })
                })
        };
        _restored = definition.ToMongoDB().ToKernel();
    }

    [Fact] void should_preserve_the_graph_mappings() => _restored.Graph.ShouldNotBeNull();
    [Fact] void should_preserve_the_node_key() => _restored.Graph!.Nodes["Person"].From["PersonRegistered+1"].Key.Value.ShouldEqual("personId");
    [Fact] void should_preserve_the_node_property_expressions() => _restored.Graph!.Nodes["Person"].From["PersonRegistered+1"].Properties["name"].ShouldEqual("name");
    [Fact] void should_preserve_update_only() => _restored.Graph!.Nodes["Person"].From["PersonRegistered+1"].UpdateOnly.ShouldBeTrue();
    [Fact] void should_preserve_detach() => _restored.Graph!.Nodes["Person"].RemovedWith["PersonRemoved+1"].Detach.ShouldBeTrue();
    [Fact] void should_preserve_the_collection_an_edge_mapping_iterates() => _restored.Graph!.Edges["MEMBER_OF"].From["MemberAdded+1"].ForEach!.Value.ShouldEqual("slots");
    [Fact] void should_preserve_the_collection_an_edge_removal_iterates() => _restored.Graph!.Edges["MEMBER_OF"].RemovedWith["MemberRemoved+1"].ForEach!.Value.ShouldEqual("slots");
    [Fact] void should_keep_a_node_mapping_without_a_collection() => _restored.Graph!.Nodes["Person"].From["PersonRegistered+1"].ForEach.ShouldBeNull();
    [Fact] void should_preserve_the_edge_identity() => _restored.Graph!.Edges["MEMBER_OF"].From["MemberAdded+1"].Identity!.Value.ShouldEqual("membershipId");
    [Fact] void should_preserve_the_edge_removal() => _restored.Graph!.Edges["MEMBER_OF"].RemovedWith["MemberRemoved+1"].TargetKey.Value.ShouldEqual("groupId");
}
