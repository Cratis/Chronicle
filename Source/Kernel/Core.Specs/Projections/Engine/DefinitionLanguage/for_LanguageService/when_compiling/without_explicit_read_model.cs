// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class without_explicit_read_model : for_LanguageService.given.a_language_service_with_schemas<for_LanguageService.given.TestModel>
{
    const string Declaration = """
        projection TestProjection
          from TestEvent
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(for_LanguageService.given.TestEvent)];

    ProjectionDefinition _result;

    void Because()
    {
        var compileResult = _languageService.Compile(
            Declaration,
            Concepts.Projections.ProjectionOwner.Client,
            [],
            _eventTypeSchemas);

        _result = compileResult.Match(
            projectionDef => projectionDef,
            errors => throw new InvalidOperationException($"Compilation failed: {string.Join(", ", errors.Errors)}"));
    }

    [Fact] void should_compile_successfully() => _result.ShouldNotBeNull();
    [Fact] void should_use_inferred_read_model_identifier() => _result.ReadModel.ShouldEqual(ReadModelIdentifier.Inferred);
    [Fact] void should_have_from_test_event() => _result.From.ContainsKey((EventType)"TestEvent").ShouldBeTrue();
}
