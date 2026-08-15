// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

/// <summary>
/// A clear names a read model property, so the validator holds it to the same rule as every other mapping: the
/// property has to exist. Skipping the check would let a declaration naming a property nobody has reach the engine.
/// </summary>
public class clear_of_a_property_that_does_not_exist : for_LanguageService.given.a_language_service_with_schemas<for_LanguageService.given.NoteReadModel>
{
    const string Declaration = """
        projection Notes => NoteReadModel
          from NoteCleared
            clear reminder
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(for_LanguageService.given.NoteCleared)];

    CompilerErrors _errors;

    void Because() => _errors = CompileExpectingErrors(Declaration);

    [Fact] void should_have_errors() => _errors.HasErrors.ShouldBeTrue();
    [Fact] void should_report_the_missing_read_model_property() => _errors.Errors.Any(_ => _.Message.Contains("Read model property 'reminder' not found")).ShouldBeTrue();
}
