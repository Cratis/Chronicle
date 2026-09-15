// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.Kotlin;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.for_KotlinProjectionCodeGenerator.when_generating;

public class a_declarative_projection : given.a_projection_to_generate
{
    KotlinProjectionCodeGenerator _generator = null!;

    void Establish() => _generator = new KotlinProjectionCodeGenerator();

    void Because() => _result = _generator.GenerateDeclarative(_definition, _readModelDefinition);

    [Fact] void should_import_the_client_builder() =>
        _result.ShouldContain("import io.cratis.chronicle.projections.IProjectionFor");

    [Fact] void should_mark_it_as_a_projection() => _result.ShouldContain("@Projection");

    [Fact] void should_declare_the_projection_against_the_read_model() =>
        _result.ShouldContain("class EmployeeProjection : IProjectionFor<Employee>");

    [Fact] void should_override_define() =>
        _result.ShouldContain("override fun define(builder: IProjectionBuilderFor<Employee>)");

    [Fact] void should_reference_events_by_class() => _result.ShouldContain(".from(EmployeeHired::class)");

    [Fact] void should_reference_read_model_properties_by_reference() =>
        _result.ShouldContain("Employee::title");
}
