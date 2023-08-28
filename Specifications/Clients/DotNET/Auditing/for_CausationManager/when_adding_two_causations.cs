// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Auditing.for_CausationManager;

public class when_adding_two_causations : Specification
{
    const string first_type = "FirstType";
    const string second_type = "SecondType";
    const string first_type_first_property = "FirstTypeFirstProperty";
    const string first_type_first_property_value = "FirstTypeFirstPropertyValue";
    const string first_type_second_property = "FirstTypeSecondProperty";
    const string first_type_second_property_value = "FirstTypeSecondPropertyValue";
    const string second_type_first_property = "SecondTypeFirstProperty";
    const string second_type_first_property_value = "SecondTypeFirstPropertyValue";
    const string second_type_second_property = "SecondTypeSecondProperty";
    const string second_type_second_property_value = "SecondTypeSecondPropertyValue";

    CausationManager manager;

    public when_adding_two_causations()
    {
        // Since the specification runner is using IAsyncLifetime - it will be in a different async context.
        // Use default behavior, since we need to have control over the async context.

        // Establish
        manager = new();

        // Because
        manager.Add(first_type, new Dictionary<string, string>
        {
            { first_type_first_property, first_type_first_property_value },
            { first_type_second_property, first_type_second_property_value }
        });

        manager.Add(second_type, new Dictionary<string, string>
        {
            { second_type_first_property, second_type_first_property_value },
            { second_type_second_property, second_type_second_property_value }
        });
    }

    [Fact] void should_have_three_causations() => manager.GetCurrentChain().Count.ShouldEqual(3);
    [Fact] void should_have_first_causation_with_correct_type() => manager.GetCurrentChain()[1].Type.Value.ShouldEqual(first_type);
    [Fact] void should_have_first_causation_with_correct_properties() => manager.GetCurrentChain()[1].Properties.Count.ShouldEqual(2);
    [Fact] void should_have_first_causation_with_correct_first_property() => manager.GetCurrentChain()[1].Properties[first_type_first_property].ShouldEqual(first_type_first_property_value);
    [Fact] void should_have_first_causation_with_correct_second_property() => manager.GetCurrentChain()[1].Properties[first_type_second_property].ShouldEqual(first_type_second_property_value);
    [Fact] void should_have_second_causation_with_correct_type() => manager.GetCurrentChain()[2].Type.Value.ShouldEqual(second_type);
    [Fact] void should_have_second_causation_with_correct_properties() => manager.GetCurrentChain()[2].Properties.Count.ShouldEqual(2);
    [Fact] void should_have_second_causation_with_correct_first_property() => manager.GetCurrentChain()[2].Properties[second_type_first_property].ShouldEqual(second_type_first_property_value);
    [Fact] void should_have_second_causation_with_correct_second_property() => manager.GetCurrentChain()[2].Properties[second_type_second_property].ShouldEqual(second_type_second_property_value);
}
