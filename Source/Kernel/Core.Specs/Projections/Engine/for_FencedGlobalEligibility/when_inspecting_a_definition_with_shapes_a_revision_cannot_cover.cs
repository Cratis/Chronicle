// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_FencedGlobalEligibility;

public class when_inspecting_a_definition_with_shapes_a_revision_cannot_cover : Specification
{
    string? _join;
    string? _children;
    string? _removal;
    string? _nestedJoin;
    string? _nestedRemoval;
    string? _childrenTwoLevelsDeep;

    void Because()
    {
        _join = FencedGlobalEligibility.ReasonFor(given.Definitions.WithJoin());
        _children = FencedGlobalEligibility.ReasonFor(given.Definitions.WithChildren());
        _removal = FencedGlobalEligibility.ReasonFor(given.Definitions.WithRemoval());
        _nestedJoin = FencedGlobalEligibility.ReasonFor(given.Definitions.WithNestedJoin());
        _nestedRemoval = FencedGlobalEligibility.ReasonFor(given.Definitions.WithNestedRemoval());
        _childrenTwoLevelsDeep = FencedGlobalEligibility.ReasonFor(given.Definitions.WithChildrenTwoLevelsDeep());
    }

    [Fact] void should_rule_out_a_join() => _join.ShouldEqual("it joins events");
    [Fact] void should_rule_out_children() => _children.ShouldEqual("it has children");
    [Fact] void should_rule_out_a_removal() => _removal.ShouldEqual("it removes with events");
    [Fact] void should_rule_out_a_join_in_a_nested_object() => _nestedJoin.ShouldEqual("it joins events");
    [Fact] void should_rule_out_a_removal_in_a_nested_object() => _nestedRemoval.ShouldEqual("it removes with events");
    [Fact] void should_rule_out_children_two_levels_deep() => _childrenTwoLevelsDeep.ShouldEqual("it has children");
}
