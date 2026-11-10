// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Services.Projections.Definitions.for_ChildrenDefinitionConverters;

public class when_round_tripping_an_ordering : Specification
{
    static readonly ChildrenOrdering _sortedDescending = new(ChildrenPlacement.Sorted, new("rank"), SortDirection.Descending);

    Contracts.Projections.ChildrenOrderingDefinition? _contractForSorted;
    ChildrenOrdering? _restoredFromChildren;
    ChildrenOrdering? _restoredWithoutOrdering;
    Contracts.Projections.ChildrenOrderingDefinition? _contractForAppend;
    ChildrenOrdering? _explicitAppendFromContract;
    ChildrenOrdering? _prependFromContract;

    void Because()
    {
        _contractForSorted = _sortedDescending.ToContract();
        _restoredFromChildren = Children(_sortedDescending).ToContract().ToChronicle().Ordering;
        _restoredWithoutOrdering = Children(null).ToContract().ToChronicle().Ordering;
        _contractForAppend = ChildrenOrdering.Append.ToContract();
        _explicitAppendFromContract = new Contracts.Projections.ChildrenOrderingDefinition { Placement = Contracts.Projections.ChildrenPlacement.Append }.ToChronicle();
        _prependFromContract = new Contracts.Projections.ChildrenOrderingDefinition { Placement = Contracts.Projections.ChildrenPlacement.Prepend }.ToChronicle();
    }

    [Fact] void should_send_the_placement() => _contractForSorted!.Placement.ShouldEqual(Contracts.Projections.ChildrenPlacement.Sorted);
    [Fact] void should_send_the_property_to_sort_by() => _contractForSorted!.SortBy.ShouldEqual("rank");
    [Fact] void should_send_the_direction() => _contractForSorted!.Direction.ShouldEqual(Contracts.Projections.SortDirection.Descending);
    [Fact] void should_keep_the_ordering_of_children() => _restoredFromChildren.ShouldEqual(_sortedDescending);
    [Fact] void should_keep_children_without_an_ordering_without_one() => _restoredWithoutOrdering.ShouldBeNull();
    [Fact] void should_send_an_explicit_append_as_no_ordering() => _contractForAppend.ShouldBeNull();
    [Fact] void should_receive_an_explicit_append_as_no_ordering() => _explicitAppendFromContract.ShouldBeNull();
    [Fact] void should_receive_a_prepend_without_a_property_to_sort_by() => _prependFromContract.ShouldEqual(new ChildrenOrdering(ChildrenPlacement.Prepend, PropertyPath.NotSet));

    static ChildrenDefinition Children(ChildrenOrdering? ordering) => new(
        "id",
        new Dictionary<EventType, FromDefinition>(),
        new Dictionary<EventType, JoinDefinition>(),
        new Dictionary<PropertyPath, ChildrenDefinition>(),
        new(new Dictionary<PropertyPath, string>(), false),
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>(),
        Ordering: ordering);
}
