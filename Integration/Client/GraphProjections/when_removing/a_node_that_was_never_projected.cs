// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.Organization;
using context = Cratis.Chronicle.Integration.GraphProjections.when_removing.a_node_that_was_never_projected.context;

namespace Cratis.Chronicle.Integration.GraphProjections.when_removing;

[Collection(GraphChronicleCollection.Name)]
public class a_node_that_was_never_projected(context context) : Given<context>(context)
{
    public class context(GraphChronicleFixture fixture) : a_model_bound_organization(fixture)
    {
        void Establish()
        {
            Tell(OrganizationStory.Cy, new OrgPersonRemoved(OrganizationStory.Cy));
        }
    }

    [Fact] void should_not_create_anything() => Context.Graph.Nodes.ShouldBeEmpty();
    [Fact] void should_not_fail() => Context.LastCompletion!.IsSuccess.ShouldBeTrue();
}
