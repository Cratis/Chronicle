// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class nested_children : given.a_language_service
{
    const string definition = """
        projection Company => CompanyReadModel
          children Departments id departmentId
            from DepartmentCreated
              key departmentId
              parent $eventContext.eventSourceId
              Name = name
            children Employees id employeeId
              from EmployeeHired
                key employeeId
                parent departmentId
                Name = name
        """;

    ProjectionDefinition _result;
    ChildrenDefinition _departmentsDef;
    ChildrenDefinition _employeesDef;

    void Because()
    {
        _result = CompileGenerateAndRecompile(definition, "OrganizationReadModel");
        _departmentsDef = _result.Children[new PropertyPath("Departments")];
        _employeesDef = _departmentsDef.Children[new PropertyPath("Employees")];
    }

    [Fact] void should_have_departments_children() => _departmentsDef.ShouldNotBeNull();
    [Fact] void should_have_nested_employees_children() => _employeesDef.ShouldNotBeNull();
    [Fact] void should_have_employees_identifier() => _employeesDef.IdentifiedBy.ShouldNotBeNull();
    [Fact] void should_have_employees_from_definition() => _employeesDef.From.ContainsKey((EventType)"EmployeeHired").ShouldBeTrue();
}
