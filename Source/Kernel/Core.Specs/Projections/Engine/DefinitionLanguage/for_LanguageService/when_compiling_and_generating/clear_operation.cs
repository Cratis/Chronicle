// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling_and_generating;

/// <summary>
/// 'clear property' is how the declaration language takes a value away. It compiles to the clear expression, and
/// the generator writes it back as a clear rather than degrading it into an assignment of the null literal -
/// assigning a value and removing one are different acts, and spelling both with '=' hides that.
/// </summary>
public class clear_operation : given.a_language_service_with_schemas<given.NoteReadModel>
{
    const string Declaration = """
        projection Notes => NoteReadModel
          from Noted
            note = note
          from NoteCleared
            clear note
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.Noted), typeof(given.NoteCleared)];

    given.CompilerResult _result;

    void Because() => _result = CompileGenerateAndRecompile(Declaration);

    [Fact] void should_compile_the_clear_to_the_clear_expression() => _result.Definition.From[(EventType)"NoteCleared"].Properties[new PropertyPath("note")].ShouldEqual(Concepts.WellKnownExpressions.Null);
    [Fact] void should_keep_the_set_on_the_other_event() => _result.Definition.From[(EventType)"Noted"].Properties[new PropertyPath("note")].ShouldEqual("note");
    [Fact] void should_generate_the_clear_statement() => _result.GeneratedDefinition.ShouldContain("clear note");
    [Fact] void should_not_degrade_the_clear_into_an_assignment_of_null() => _result.GeneratedDefinition.ShouldNotContain("note = null");
}
