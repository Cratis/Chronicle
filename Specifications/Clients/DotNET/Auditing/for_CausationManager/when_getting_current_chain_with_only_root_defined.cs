// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Auditing.for_CausationManager;

public class when_getting_current_chain_with_only_root_defined : Specification
{
    const string root_first_property = "RootFirstProperty";
    const string root_first_property_value = "RootFirstPropertyValue";
    const string root_second_property = "RootSecondProperty";
    const string root_second_property_value = "RootSecondPropertyValue";

    CausationManager manager;

    public when_getting_current_chain_with_only_root_defined()
    {
        CausationManager.DefineRoot(new Dictionary<string, string>
        {
            { root_first_property, root_first_property_value },
            { root_second_property, root_second_property_value }
        });
        manager = new();
    }

    [Fact] void should_have_one_causation() => manager.GetCurrentChain().Count.ShouldEqual(1);
    [Fact] void should_have_root_causation() => manager.GetCurrentChain()[0].Type.ShouldEqual(CausationType.Root);
    [Fact] void should_have_root_causation_with_correct_properties() => manager.GetCurrentChain()[0].Properties.Count.ShouldEqual(2);
    [Fact] void should_have_root_causation_with_correct_first_property() => manager.GetCurrentChain()[0].Properties[root_first_property].ShouldEqual(root_first_property_value);
    [Fact] void should_have_root_causation_with_correct_second_property() => manager.GetCurrentChain()[0].Properties[root_second_property].ShouldEqual(root_second_property_value);
}
