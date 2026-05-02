// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class without_explicit_read_model_and_incompatible_event_property_types : for_LanguageService.given.a_language_service_with_schemas<for_LanguageService.given.TestModel>
{
    const string Declaration = """
        projection ConflictingProjection
          from TestEvent
          from EventA
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(for_LanguageService.given.TestEvent), typeof(for_LanguageService.given.EventA)];

    CompilerErrors _errors;

    void Because()
    {
        var compileResult = _languageService.Compile(
            Declaration,
            Concepts.Projections.ProjectionOwner.Client,
            [],
            _eventTypeSchemas);

        _errors = compileResult.Match(
            _ => CompilerErrors.Empty,
            errors => errors);
    }

    [Fact] void should_have_errors() => _errors.HasErrors.ShouldBeTrue();
    [Fact] void should_report_type_incompatibility() => _errors.Errors.ShouldContain(e => e.Message.Contains("incompatible"));
}
