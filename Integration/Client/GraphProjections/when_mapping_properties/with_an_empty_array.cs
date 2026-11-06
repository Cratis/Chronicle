// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.Organization;
using context = Cratis.Chronicle.Integration.GraphProjections.when_mapping_properties.with_an_empty_array.context;

namespace Cratis.Chronicle.Integration.GraphProjections.when_mapping_properties;

[Collection(GraphChronicleCollection.Name)]
public class with_an_empty_array(context context) : Given<context>(context)
{
    public class context(GraphChronicleFixture fixture) : a_model_bound_organization(fixture)
    {
        void Establish()
        {
            Tell(OrganizationStory.Cy, new OrgPersonRegistered(OrganizationStory.Cy, "Cy", []));
        }
    }

    [Fact] void should_store_an_empty_array() => ((IEnumerable<object>)Context.Graph.Node("Person", "personId", OrganizationStory.Cy)!["tags"]!).ShouldBeEmpty();
}
