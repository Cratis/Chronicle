// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.TypeScript;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.for_TypeScriptProjectionCodeGenerator.when_generating;

public class a_declarative_projection : given.a_projection_to_generate
{
    TypeScriptProjectionCodeGenerator _generator = null!;

    void Establish() => _generator = new TypeScriptProjectionCodeGenerator();

    void Because() => _result = _generator.GenerateDeclarative(_definition, _readModelDefinition);

    [Fact] void should_import_the_client_builder() => _result.ShouldContain("from '@cratis/chronicle'");

    [Fact] void should_declare_the_projection_against_the_read_model() =>
        _result.ShouldContain("export class EmployeeProjection implements IProjectionFor<Employee>");

    [Fact] void should_define_against_the_builder() =>
        _result.ShouldContain("define(builder: IProjectionBuilderFor<Employee>): void");

    [Fact] void should_read_from_each_event() => _result.ShouldContain(".from(EmployeeHired, fb => fb");

    [Fact] void should_camel_case_read_model_properties() => _result.ShouldContain("m => m.title");

    [Fact] void should_camel_case_event_properties() => _result.ShouldContain("e => e.newTitle");

    [Fact] void should_count_without_a_value() => _result.ShouldContain(".count(m => m.promotionCount)");
}
