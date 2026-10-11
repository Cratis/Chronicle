// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class with_wrong_read_model_identifier_casing : for_LanguageService.given.a_language_service_with_qualified_read_models
{
    void Because() => CompileExpectingErrors("projection Orders => example.readmodels.order\n  from OrderPlaced\n");

    [Fact] void should_reject_the_wrong_casing() => _errors.HasErrors.ShouldBeTrue();
    [Fact] void should_suggest_the_identifier() => _errors.Errors.Single().Message.ShouldEqual($"Read model 'example.readmodels.order' not found; did you mean '{ReadModelName}'?");
}
