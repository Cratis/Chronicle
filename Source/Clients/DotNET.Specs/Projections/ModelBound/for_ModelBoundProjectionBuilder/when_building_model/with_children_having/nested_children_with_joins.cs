// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_having;

public class nested_children_with_joins : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(JoinedOrgCreated),
            typeof(JoinedDeptAdded),
            typeof(JoinedDeptRenamed),
            typeof(JoinedTeamAdded),
            typeof(JoinedTeamRenamed)
        ]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(JoinedOrganization));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact]
    void should_have_children_for_departments()
    {
        _result.Children.Keys.ShouldContain(nameof(JoinedOrganization.Departments));
    }

    [Fact]
    void should_have_join_for_department_renamed_on_first_level_children()
    {
        var eventType = event_types.GetEventTypeFor(typeof(JoinedDeptRenamed)).ToContract();
        var deptChildrenDef = _result.Children[nameof(JoinedOrganization.Departments)];
        deptChildrenDef.Join.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_nested_children_for_teams()
    {
        var deptChildrenDef = _result.Children[nameof(JoinedOrganization.Departments)];
        deptChildrenDef.Children.Keys.ShouldContain(nameof(JoinedDepartment.Teams));
    }

    [Fact]
    void should_have_join_for_team_renamed_on_nested_children()
    {
        var eventType = event_types.GetEventTypeFor(typeof(JoinedTeamRenamed)).ToContract();
        var deptChildrenDef = _result.Children[nameof(JoinedOrganization.Departments)];
        var teamChildrenDef = deptChildrenDef.Children[nameof(JoinedDepartment.Teams)];
        teamChildrenDef.Join.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_map_name_from_join_on_nested_children()
    {
        var eventType = event_types.GetEventTypeFor(typeof(JoinedTeamRenamed)).ToContract();
        var deptChildrenDef = _result.Children[nameof(JoinedOrganization.Departments)];
        var teamChildrenDef = deptChildrenDef.Children[nameof(JoinedDepartment.Teams)];
        var joinDef = teamChildrenDef.Join.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        joinDef.Properties.Keys.ShouldContain(nameof(JoinedTeam.Name));
    }
}

[EventType]
public record JoinedOrgCreated(string Name);

[EventType]
public record JoinedDeptAdded(JoinedDeptId Id, string Name);

[EventType]
public record JoinedDeptRenamed(JoinedDeptId Id, string Name);

[EventType]
public record JoinedTeamAdded(JoinedTeamId Id, JoinedDeptId DepartmentId, string Name);

[EventType]
public record JoinedTeamRenamed(JoinedTeamId Id, string Name);

public record JoinedOrgId(Guid Value);
public record JoinedDeptId(Guid Value);
public record JoinedTeamId(Guid Value);

public record JoinedTeam(
    [Key] JoinedTeamId Id,

    [SetFrom<JoinedTeamAdded>]
    [Join<JoinedTeamRenamed>(nameof(JoinedTeamRenamed.Id))]
    string Name);

public record JoinedDepartment(
    [Key] JoinedDeptId Id,

    [SetFrom<JoinedDeptAdded>]
    [Join<JoinedDeptRenamed>(nameof(JoinedDeptRenamed.Id))]
    string Name,

    [ChildrenFrom<JoinedTeamAdded>(key: nameof(JoinedTeamAdded.Id), identifiedBy: nameof(JoinedTeam.Id), parentKey: nameof(JoinedTeamAdded.DepartmentId))]
    IEnumerable<JoinedTeam> Teams);

[Passive]
[FromEvent<JoinedOrgCreated>]
public record JoinedOrganization(
    JoinedOrgId Id,
    string Name,

    [ChildrenFrom<JoinedDeptAdded>(key: nameof(JoinedDeptAdded.Id), identifiedBy: nameof(JoinedDepartment.Id))]
    IEnumerable<JoinedDepartment> Departments);
