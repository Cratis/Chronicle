// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;
using Kernel = Cratis.Chronicle.Concepts.Projections.Definitions;
using KernelChildren = Cratis.Chronicle.Concepts.Projections.Definitions.ChildrenDefinition;
using KernelEventType = Cratis.Chronicle.Concepts.Events.EventType;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ChildrenDefinitionConverters;

public class when_round_tripping_an_ordering : Specification
{
    static readonly ChildrenOrdering _sortedDescending = new(ChildrenPlacement.Sorted, new("rank"), SortDirection.Descending);
    static readonly ChildrenOrdering _prepended = new(ChildrenPlacement.Prepend, PropertyPath.NotSet);

    ChildrenOrdering? _restoredSorted;
    ChildrenOrdering? _restoredPrepended;
    ChildrenOrderingDefinition? _storedAppend;
    ChildrenOrdering? _restoredFromChildren;
    ChildrenOrdering? _restoredWithoutOrdering;

    void Because()
    {
        _restoredSorted = _sortedDescending.ToMongoDB()!.ToKernel();
        _restoredPrepended = _prepended.ToMongoDB()!.ToKernel();
        _storedAppend = ChildrenOrdering.Append.ToMongoDB();
        _restoredFromChildren = Children(_sortedDescending).ToMongoDB().ToKernel().Ordering;
        _restoredWithoutOrdering = Children(null).ToMongoDB().ToKernel().Ordering;
    }

    [Fact] void should_keep_a_sorted_ordering() => _restoredSorted.ShouldEqual(_sortedDescending);
    [Fact] void should_keep_a_prepend_ordering() => _restoredPrepended!.Placement.ShouldEqual(ChildrenPlacement.Prepend);
    [Fact] void should_store_an_explicit_append_as_no_ordering() => _storedAppend.ShouldBeNull();
    [Fact] void should_keep_the_ordering_of_children() => _restoredFromChildren.ShouldEqual(_sortedDescending);
    [Fact] void should_keep_children_without_an_ordering_without_one() => _restoredWithoutOrdering.ShouldBeNull();

    static KernelChildren Children(ChildrenOrdering? ordering) => new(
        "id",
        new Dictionary<KernelEventType, Kernel.FromDefinition>(),
        new Dictionary<KernelEventType, Kernel.JoinDefinition>(),
        new Dictionary<PropertyPath, KernelChildren>(),
        new(new Dictionary<PropertyPath, string>(), false),
        new Dictionary<KernelEventType, Kernel.RemovedWithDefinition>(),
        new Dictionary<KernelEventType, Kernel.RemovedWithJoinDefinition>(),
        Ordering: ordering);
}
