// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling_and_generating;

/// <summary>
/// 'property = null' is the spelling a released Chronicle already accepts, so it keeps compiling to exactly the
/// same clear as 'clear property'. Adding the statement is additive - the old spelling is never renamed away,
/// it just stops being the one the generator writes back.
/// </summary>
public class clear_operation_written_as_a_null_literal : given.a_language_service_with_schemas<given.NoteReadModel>
{
    const string Declaration = """
        projection Notes => NoteReadModel
          from NoteCleared
            note = null
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.NoteCleared)];

    given.CompilerResult _result;

    void Because() => _result = CompileGenerateAndRecompile(Declaration);

    [Fact] void should_compile_the_null_literal_to_the_clear_expression() => _result.Definition.From[(EventType)"NoteCleared"].Properties[new PropertyPath("note")].ShouldEqual(Concepts.WellKnownExpressions.Null);
    [Fact] void should_generate_the_clear_statement() => _result.GeneratedDefinition.ShouldContain("clear note");
    [Fact] void should_not_generate_an_assignment_of_null() => _result.GeneratedDefinition.ShouldNotContain("note = null");
}
