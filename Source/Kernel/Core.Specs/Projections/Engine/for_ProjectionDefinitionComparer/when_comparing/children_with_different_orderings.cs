// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionDefinitionComparer.when_comparing;

public class children_with_different_orderings : given.a_projection_definition_comparer
{
    ProjectionDefinitionCompareResult _withoutAndSorted;
    ProjectionDefinitionCompareResult _ascendingAndDescending;

    async Task Because()
    {
        _projectionsStorage.Has(_projectionKey.ProjectionId).Returns(true);
        var ascending = new ChildrenOrdering(ChildrenPlacement.Sorted, new("rank"));
        _withoutAndSorted = await _comparer.Compare(_projectionKey, Children(null), Children(ascending));
        _ascendingAndDescending = await _comparer.Compare(_projectionKey, Children(ascending), Children(ascending with { Direction = SortDirection.Descending }));
    }

    [Fact] void should_detect_an_ordering_added_to_children_without_one() => _withoutAndSorted.ShouldEqual(ProjectionDefinitionCompareResult.Different);
    [Fact] void should_detect_a_changed_direction() => _ascendingAndDescending.ShouldEqual(ProjectionDefinitionCompareResult.Different);

    ProjectionDefinition Children(ChildrenOrdering? ordering) => CreateDefinition(children: new Dictionary<PropertyPath, ChildrenDefinition>
    {
        ["items"] = new(
            "id",
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            new(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            Ordering: ordering)
    });
}
