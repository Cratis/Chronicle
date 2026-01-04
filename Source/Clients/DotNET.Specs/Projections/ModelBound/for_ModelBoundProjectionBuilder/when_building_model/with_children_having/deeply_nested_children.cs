// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_having;

public class deeply_nested_children : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(DeepCompanyCreated),
            typeof(DeepDepartmentAdded),
            typeof(DeepTeamAdded),
            typeof(DeepMemberAdded)
        ]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(DeepCompany));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact]
    void should_have_children_for_departments()
    {
        _result.Children.Keys.ShouldContain(nameof(DeepCompany.Departments));
    }

    [Fact]
    void should_have_nested_children_for_teams()
    {
        var deptChildrenDef = _result.Children[nameof(DeepCompany.Departments)];
        deptChildrenDef.Children.Keys.ShouldContain(nameof(DeepDepartment.Teams));
    }

    [Fact]
    void should_have_deeply_nested_children_for_members()
    {
        var deptChildrenDef = _result.Children[nameof(DeepCompany.Departments)];
        var teamChildrenDef = deptChildrenDef.Children[nameof(DeepDepartment.Teams)];
        teamChildrenDef.Children.Keys.ShouldContain(nameof(DeepTeam.Members));
    }

    [Fact]
    void should_have_from_definition_for_member_added_in_deeply_nested_children()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DeepMemberAdded)).ToContract();
        var deptChildrenDef = _result.Children[nameof(DeepCompany.Departments)];
        var teamChildrenDef = deptChildrenDef.Children[nameof(DeepDepartment.Teams)];
        var memberChildrenDef = teamChildrenDef.Children[nameof(DeepTeam.Members)];
        memberChildrenDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_auto_map_name_property_for_deeply_nested_child()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DeepMemberAdded)).ToContract();
        var deptChildrenDef = _result.Children[nameof(DeepCompany.Departments)];
        var teamChildrenDef = deptChildrenDef.Children[nameof(DeepDepartment.Teams)];
        var memberChildrenDef = teamChildrenDef.Children[nameof(DeepTeam.Members)];
        var fromDef = memberChildrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldContain(nameof(DeepMember.Name));
    }
}

[EventType]
public record DeepCompanyCreated(string Name);

[EventType]
public record DeepDepartmentAdded(DeepDepartmentId Id, string Name);

[EventType]
public record DeepTeamAdded(DeepTeamId Id, DeepDepartmentId DepartmentId, string Name);

[EventType]
public record DeepMemberAdded(DeepMemberId Id, DeepTeamId TeamId, string Name);

public record DeepCompanyId(Guid Value);
public record DeepDepartmentId(Guid Value);
public record DeepTeamId(Guid Value);
public record DeepMemberId(Guid Value);

public record DeepMember(
    [Key] DeepMemberId Id,
    string Name);

public record DeepTeam(
    [Key] DeepTeamId Id,
    string Name,

    [ChildrenFrom<DeepMemberAdded>(key: nameof(DeepMemberAdded.Id), identifiedBy: nameof(DeepMember.Id), parentKey: nameof(DeepMemberAdded.TeamId))]
    IEnumerable<DeepMember> Members);

public record DeepDepartment(
    [Key] DeepDepartmentId Id,
    string Name,

    [ChildrenFrom<DeepTeamAdded>(key: nameof(DeepTeamAdded.Id), identifiedBy: nameof(DeepTeam.Id), parentKey: nameof(DeepTeamAdded.DepartmentId))]
    IEnumerable<DeepTeam> Teams);

[Passive]
[FromEvent<DeepCompanyCreated>]
public record DeepCompany(
    DeepCompanyId Id,
    string Name,

    [ChildrenFrom<DeepDepartmentAdded>(key: nameof(DeepDepartmentAdded.Id), identifiedBy: nameof(DeepDepartment.Id))]
    IEnumerable<DeepDepartment> Departments);
