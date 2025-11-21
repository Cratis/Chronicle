// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_having;

public class key_attribute_should_default_to_event_source_id : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(EmployeeHired)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(Department));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_children_definition() => _result.Children.Count.ShouldEqual(1);

    [Fact] void should_have_children_for_employees()
    {
        _result.Children.Keys.ShouldContain(nameof(Department.Employees));
    }

    [Fact] void should_have_from_definition_for_employee_hired()
    {
        var eventType = event_types.GetEventTypeFor(typeof(EmployeeHired)).ToContract();
        var childrenDef = _result.Children[nameof(Department.Employees)];
        childrenDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact] void should_map_employee_number_property_to_event_source_id_by_default()
    {
        var eventType = event_types.GetEventTypeFor(typeof(EmployeeHired)).ToContract();
        var childrenDef = _result.Children[nameof(Department.Employees)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldContain(nameof(Employee.EmployeeNumber));
        fromDef.Properties[nameof(Employee.EmployeeNumber)].ShouldEqual("$eventContext(EventSourceId)");
    }

    [Fact] void should_auto_map_name_property()
    {
        var eventType = event_types.GetEventTypeFor(typeof(EmployeeHired)).ToContract();
        var childrenDef = _result.Children[nameof(Department.Employees)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldContain(nameof(Employee.Name));
    }

    [Fact] void should_apply_naming_policy_to_identified_by()
    {
        var childrenDef = _result.Children[nameof(Department.Employees)];
        childrenDef.IdentifiedBy.ShouldEqual(naming_policy.GetPropertyName(new Properties.PropertyPath(nameof(Employee.EmployeeNumber))));
    }
}

[EventType]
public record EmployeeHired(string Name, string Position);

public record EmployeeNumber(Guid Value);
public record DepartmentId(Guid Value);

public record Employee(
    [Key] EmployeeNumber EmployeeNumber,
    string Name,
    string Position);

[Passive]
[FromEvent<EmployeeHired>]
public record Department(
    DepartmentId Id,

    [ChildrenFrom<EmployeeHired>(identifiedBy: nameof(Employee.EmployeeNumber))]
    IEnumerable<Employee> Employees);
