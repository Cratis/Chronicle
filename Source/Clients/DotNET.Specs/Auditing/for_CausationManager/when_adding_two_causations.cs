// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationManager;

public class when_adding_two_causations : Specification
{
    const string FirstType = "FirstType";
    const string SecondType = "SecondType";
    const string FirstTypeFirstProperty = "FirstTypeFirstProperty";
    const string FirstTypeFirstPropertyValue = "FirstTypeFirstPropertyValue";
    const string FirstTypeSecondProperty = "FirstTypeSecondProperty";
    const string FirstTypeSecondPropertyValue = "FirstTypeSecondPropertyValue";
    const string SecondTypeFirstProperty = "SecondTypeFirstProperty";
    const string SecondTypeFirstPropertyValue = "SecondTypeFirstPropertyValue";
    const string SecondTypeSecondProperty = "SecondTypeSecondProperty";
    const string SecondTypeSecondPropertyValue = "SecondTypeSecondPropertyValue";

    CausationManager _manager;

    public when_adding_two_causations()
    {
        // Since the specification runner is using IAsyncLifetime - it will be in a different async context.
        // Use default behavior, since we need to have control over the async context.

        // Establish
        _manager = new();

        // Because
        _manager.Add(FirstType, new Dictionary<string, string>
        {
            { FirstTypeFirstProperty, FirstTypeFirstPropertyValue },
            { FirstTypeSecondProperty, FirstTypeSecondPropertyValue }
        });

        _manager.Add(SecondType, new Dictionary<string, string>
        {
            { SecondTypeFirstProperty, SecondTypeFirstPropertyValue },
            { SecondTypeSecondProperty, SecondTypeSecondPropertyValue }
        });
    }

    [Fact] void should_have_three_causations() => _manager.GetCurrentChain().Count.ShouldEqual(3);
    [Fact] void should_have_first_causation_with_correct_type() => _manager.GetCurrentChain()[1].Type.Value.ShouldEqual(FirstType);
    [Fact] void should_have_first_causation_with_correct_properties() => _manager.GetCurrentChain()[1].Properties.Count.ShouldEqual(2);
    [Fact] void should_have_first_causation_with_correct_first_property() => _manager.GetCurrentChain()[1].Properties[FirstTypeFirstProperty].ShouldEqual(FirstTypeFirstPropertyValue);
    [Fact] void should_have_first_causation_with_correct_second_property() => _manager.GetCurrentChain()[1].Properties[FirstTypeSecondProperty].ShouldEqual(FirstTypeSecondPropertyValue);
    [Fact] void should_have_second_causation_with_correct_type() => _manager.GetCurrentChain()[2].Type.Value.ShouldEqual(SecondType);
    [Fact] void should_have_second_causation_with_correct_properties() => _manager.GetCurrentChain()[2].Properties.Count.ShouldEqual(2);
    [Fact] void should_have_second_causation_with_correct_first_property() => _manager.GetCurrentChain()[2].Properties[SecondTypeFirstProperty].ShouldEqual(SecondTypeFirstPropertyValue);
    [Fact] void should_have_second_causation_with_correct_second_property() => _manager.GetCurrentChain()[2].Properties[SecondTypeSecondProperty].ShouldEqual(SecondTypeSecondPropertyValue);
}
