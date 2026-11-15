// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.Fluent;
using Cratis.Chronicle.Integration.GraphProjections.Organization;
using context = Cratis.Chronicle.Integration.GraphProjections.when_projecting_the_organization.with_the_fluent_builder.context;

namespace Cratis.Chronicle.Integration.GraphProjections.when_projecting_the_organization;

[Collection(GraphChronicleCollection.Name)]
public class with_the_fluent_builder(context context) : Given<context>(context)
{
    public class context(GraphChronicleFixture fixture) : a_told_organization_story(fixture)
    {
        public override IEnumerable<Type> GraphProjections => [typeof(FluentOrganization)];

        protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(new FluentOrganization());
    }

    [Fact] void should_leave_the_expected_graph() => Assert.Equal(ExpectedOrganization.Graph, Context.Graph.Describe());
    [Fact] void should_have_three_kinds_of_nodes_in_the_right_numbers() => (Context.Graph.NodesOf("Person").Count(), Context.Graph.NodesOf("Group").Count(), Context.Graph.NodesOf("Place").Count()).ShouldEqual((2, 2, 2));
    [Fact] void should_not_have_the_removed_person() => Context.Graph.Node("Person", "personId", OrganizationStory.Cy).ShouldBeNull();
    [Fact] void should_own_every_node_with_the_projection() => Context.Graph.Nodes.ShouldEachConformTo(_ => _.Owner is not null);
    [Fact] void should_stamp_the_namespace_in_single_database_mode_only() => Context.Graph.Nodes.ShouldEachConformTo(_ => Context.IsSingleDatabase == (_.Namespace == "Default"));
}
