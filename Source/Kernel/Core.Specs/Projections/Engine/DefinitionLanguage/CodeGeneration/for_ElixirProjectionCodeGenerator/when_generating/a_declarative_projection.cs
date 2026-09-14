// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.Elixir;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.for_ElixirProjectionCodeGenerator.when_generating;

public class a_declarative_projection : given.a_projection_to_generate
{
    ElixirProjectionCodeGenerator _generator = null!;

    void Establish() => _generator = new ElixirProjectionCodeGenerator();

    void Because() => _result = _generator.GenerateDeclarative(_definition, _readModelDefinition);

    [Fact] void should_declare_a_projection_module() => _result.ShouldContain("defmodule EmployeeProjection do");

    [Fact] void should_name_the_read_model_it_targets() =>
        _result.ShouldContain("use Chronicle.Projections.Projection, model: Employee");

    [Fact] void should_alias_the_events() => _result.ShouldContain("alias Events.{EmployeeHired, EmployeePromoted}");

    [Fact] void should_read_from_each_event() => _result.ShouldContain("from EmployeeHired");

    [Fact] void should_snake_case_a_renamed_mapping() => _result.ShouldContain("set: [title: :new_title]");

    [Fact] void should_snake_case_a_count() => _result.ShouldContain("count: :promotion_count");
}
