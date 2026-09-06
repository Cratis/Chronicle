// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.TypeScript;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.for_TypeScriptProjectionCodeGenerator.when_generating;

public class a_model_bound_read_model : given.a_projection_to_generate
{
    TypeScriptProjectionCodeGenerator _generator = null!;

    void Establish() => _generator = new TypeScriptProjectionCodeGenerator();

    void Because() => _result = _generator.GenerateModelBound(_definition, _readModelDefinition);

    [Fact] void should_mark_the_class_as_a_read_model() => _result.ShouldContain("@readModel()");

    [Fact] void should_declare_the_read_model() => _result.ShouldContain("export class Employee {");

    [Fact] void should_carry_a_decorator_per_event() => _result.ShouldContain("@fromEvent(EmployeeHired)");

    [Fact] void should_map_a_renamed_property_from_its_event() =>
        _result.ShouldContain("@setFrom(EmployeePromoted, 'newTitle')");

    [Fact] void should_count_from_its_event() => _result.ShouldContain("@count(EmployeePromoted)");

    [Fact] void should_declare_properties_with_defaults() => _result.ShouldContain("firstName: string = '';");
}
