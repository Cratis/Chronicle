// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling_and_generating;

/// <summary>
/// The clear statement is a new front door onto the mapping the client code generators already render, so a
/// declaration written with 'clear' reaches the fluent Clear and the ClearWith attribute without either generator
/// knowing the statement exists.
/// </summary>
public class clear_operation_as_client_code : given.a_language_service_with_schemas<given.NoteReadModel>
{
    const string Declaration = """
        projection Notes => NoteReadModel
          from NoteCleared
            clear note
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.NoteCleared)];

    string _declarativeCode;
    string _modelBoundCode;

    void Because()
    {
        var result = CompileGenerateAndRecompile(Declaration);
        _declarativeCode = _languageService.GenerateDeclarativeCode(result, _readModelDefinition);
        _modelBoundCode = _languageService.GenerateModelBoundCode(result, _readModelDefinition);
    }

    [Fact] void should_render_the_fluent_clear() => _declarativeCode.ShouldContain(".Clear(m => m.note)");
    [Fact] void should_not_render_the_fluent_clear_as_a_set() => _declarativeCode.ShouldNotContain(".Set(m => m.note)");
    [Fact] void should_render_the_model_bound_clear_with() => _modelBoundCode.ShouldContain("ClearWith<NoteCleared>");
    [Fact] void should_not_render_the_model_bound_clear_as_a_set_value() => _modelBoundCode.ShouldNotContain("SetValue<NoteCleared>");
}
