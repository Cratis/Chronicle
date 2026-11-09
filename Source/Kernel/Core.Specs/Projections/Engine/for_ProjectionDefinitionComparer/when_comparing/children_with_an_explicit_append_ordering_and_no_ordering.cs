// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionDefinitionComparer.when_comparing;

public class children_with_an_explicit_append_ordering_and_no_ordering : given.a_projection_definition_comparer
{
    ProjectionDefinitionCompareResult _forward;
    ProjectionDefinitionCompareResult _reverse;

    async Task Because()
    {
        _projectionsStorage.Has(_projectionKey.ProjectionId).Returns(true);
        var withoutOrdering = Children(null);
        var withAppend = Children(ChildrenOrdering.Append);
        _forward = await _comparer.Compare(_projectionKey, withoutOrdering, withAppend);
        _reverse = await _comparer.Compare(_projectionKey, withAppend, withoutOrdering);
    }

    [Fact] void should_compare_as_unchanged() => _forward.ShouldEqual(ProjectionDefinitionCompareResult.Same);
    [Fact] void should_be_symmetric() => _reverse.ShouldEqual(ProjectionDefinitionCompareResult.Same);

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
