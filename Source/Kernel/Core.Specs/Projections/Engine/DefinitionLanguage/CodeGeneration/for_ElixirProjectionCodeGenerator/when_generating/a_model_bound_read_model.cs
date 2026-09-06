// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.Elixir;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.for_ElixirProjectionCodeGenerator.when_generating;

public class a_model_bound_read_model : given.a_projection_to_generate
{
    ElixirProjectionCodeGenerator _generator = null!;

    void Establish() => _generator = new ElixirProjectionCodeGenerator();

    void Because() => _result = _generator.GenerateModelBound(_definition, _readModelDefinition);

    [Fact] void should_declare_the_read_model_module() => _result.ShouldContain("defmodule Employee do");

    [Fact] void should_use_the_read_model_macro() => _result.ShouldContain("use Chronicle.ReadModels.ReadModel");

    [Fact] void should_declare_a_struct_of_its_fields() => _result.ShouldContain("defstruct id: \"\",");

    [Fact] void should_snake_case_its_fields() => _result.ShouldContain("promotion_count: 0");

    [Fact] void should_carry_the_from_macros() => _result.ShouldContain("from EmployeePromoted");
}
