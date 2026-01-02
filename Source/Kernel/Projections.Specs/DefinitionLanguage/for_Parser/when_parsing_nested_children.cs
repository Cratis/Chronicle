// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.DefinitionLanguage.AST;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_Parser;

public class when_parsing_nested_children : Specification
{
    const string definition = """
        projection Company => CompanyReadModel
          children Departments id e.departmentId
            from DepartmentCreated
              key e.departmentId
              parent ctx.eventSourceId
              Name = e.name
            children Employees id e.employeeId
              from EmployeeHired
                key e.employeeId
                parent e.departmentId
                Name = e.name
        """;

    ChildrenBlock _departmentsBlock;
    NestedChildrenBlock _employeesBlock;

    void Because()
    {
        var tokenizer = new Tokenizer(definition);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        var parseResult = parser.Parse();
        var result = parseResult.Match(doc => doc, errors => throw new InvalidOperationException($"Parsing failed: {string.Join(", ", errors.Errors)}"));
        _departmentsBlock = (ChildrenBlock)result.Projections[0].Directives[0];
        _employeesBlock = (NestedChildrenBlock)_departmentsBlock.ChildBlocks[1];
    }

    [Fact] void should_have_departments_children() => _departmentsBlock.ShouldNotBeNull();
    [Fact] void should_have_nested_employees_children() => _employeesBlock.ShouldNotBeNull();
    [Fact] void should_have_employees_collection_name() => _employeesBlock.CollectionName.ShouldEqual("Employees");
    [Fact] void should_have_employees_child_blocks() => _employeesBlock.ChildBlocks.Count.ShouldBeGreaterThan(0);
}
