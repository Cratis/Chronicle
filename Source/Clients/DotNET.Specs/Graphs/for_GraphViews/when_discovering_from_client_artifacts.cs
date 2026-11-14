// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs.for_GraphViewReader.given;

namespace Cratis.Chronicle.Graphs.for_GraphViews;

public class when_discovering_from_client_artifacts : Specification
{
    IClientArtifactsProvider _artifacts;
    GraphViews _views;

    void Establish()
    {
        _artifacts = Substitute.For<IClientArtifactsProvider>();
        _artifacts.GraphElements.Returns(a_graph_view_reader.OrganizationSchema);
        _artifacts.GraphProjections.Returns([]);
        _artifacts.GraphViews.Returns(a_graph_view_reader.OrganizationViews);
    }

    void Because() => _views = GraphViews.Discover(_artifacts);

    [Fact] void should_read_every_view() => a_graph_view_reader.OrganizationViews.All(_views.IsView).ShouldBeTrue();
    [Fact] void should_resolve_the_graph_of_a_view() => _views.GraphOf(typeof(PersonWithGroups)).ShouldEqual("Organization");
    [Fact] void should_resolve_a_view_by_name() => _views.NodeOf("Organization", "PersonRoles").ShouldBeSame(_views.NodeOf(typeof(PersonRoles)));
    [Fact] void should_carry_the_identity_of_an_identified_edge() => _views.NodeOf(typeof(PersonRoles)).Members.Single(_ => _.Name == "roles").Relation!.IdentifyingProperty.ShouldEqual("assignmentId");
}
