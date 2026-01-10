// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_no_auto_map;

public class on_child_only : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(CompanyCreated),
            typeof(DepartmentAdded)
        ]);

        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(CompanyWithNoAutoMapChild));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_children_definition() => _result.Children.Count.ShouldEqual(1);

    [Fact]
    void should_auto_map_root_properties()
    {
        var eventType = event_types.GetEventTypeFor(typeof(CompanyCreated)).ToContract();
        var fromDef = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;

        // Root should still auto-map
        fromDef.Properties.Keys.ShouldContain(nameof(CompanyWithNoAutoMapChild.Name));
    }

    [Fact]
    void should_not_auto_map_child_properties()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DepartmentAdded)).ToContract();
        var childrenDef = _result.Children[nameof(CompanyWithNoAutoMapChild.Departments)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;

        // Child has NoAutoMap, so no properties should be auto-mapped
        fromDef.Properties.Count.ShouldEqual(0);
    }
}

[EventType]
public record CompanyCreated(string Name);

[EventType]
public record DepartmentAdded(Guid DepartmentId, string DepartmentName);

[NoAutoMap]
public record DepartmentWithNoAutoMap([Key] Guid Id, string DepartmentName);

[FromEvent<CompanyCreated>]
public record CompanyWithNoAutoMapChild(
    [Key] Guid Id,
    string Name,
    [ChildrenFrom<DepartmentAdded>(key: nameof(DepartmentAdded.DepartmentId))]
    IEnumerable<DepartmentWithNoAutoMap> Departments);
