// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationManager;

public class when_getting_current_chain_with_only_root_defined : Specification
{
    const string RootFirstProperty = "RootFirstProperty";
    const string RootFirstPropertyValue = "RootFirstPropertyValue";
    const string RootSecondProperty = "RootSecondProperty";
    const string RootSecondPropertyValue = "RootSecondPropertyValue";

    CausationManager _manager;

    public when_getting_current_chain_with_only_root_defined()
    {
        _manager = new();
        _manager.DefineRoot(new Dictionary<string, string>
        {
            { RootFirstProperty, RootFirstPropertyValue },
            { RootSecondProperty, RootSecondPropertyValue }
        });
    }

    [Fact] void should_have_one_causation() => _manager.GetCurrentChain().Count.ShouldEqual(1);
    [Fact] void should_have_root_causation() => _manager.GetCurrentChain()[0].Type.ShouldEqual(CausationType.Root);
    [Fact] void should_have_root_causation_with_correct_properties() => _manager.GetCurrentChain()[0].Properties.Count.ShouldEqual(2);
    [Fact] void should_have_root_causation_with_correct_first_property() => _manager.GetCurrentChain()[0].Properties[RootFirstProperty].ShouldEqual(RootFirstPropertyValue);
    [Fact] void should_have_root_causation_with_correct_second_property() => _manager.GetCurrentChain()[0].Properties[RootSecondProperty].ShouldEqual(RootSecondPropertyValue);
}
