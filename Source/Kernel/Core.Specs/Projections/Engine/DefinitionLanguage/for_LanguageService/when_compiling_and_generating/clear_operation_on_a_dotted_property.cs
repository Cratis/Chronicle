// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling_and_generating;

/// <summary>
/// A clear names a property path, not just a top-level property, so a member of a nested object clears the same
/// way and round-trips with its path intact.
/// </summary>
public class clear_operation_on_a_dotted_property : given.a_language_service_with_schemas<given.NoteReadModel>
{
    const string Declaration = """
        projection Notes => NoteReadModel
          from NoteCleared
            clear owner.note
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.NoteCleared)];

    given.CompilerResult _result;

    void Because() => _result = CompileGenerateAndRecompile(Declaration);

    [Fact] void should_compile_the_clear_to_the_clear_expression() => _result.Definition.From[(EventType)"NoteCleared"].Properties[new PropertyPath("owner.note")].ShouldEqual(Concepts.WellKnownExpressions.Null);
    [Fact] void should_generate_the_clear_statement_with_the_full_path() => _result.GeneratedDefinition.ShouldContain("clear owner.note");
    [Fact] void should_not_degrade_the_clear_into_an_assignment_of_null() => _result.GeneratedDefinition.ShouldNotContain("owner.note = null");
}
