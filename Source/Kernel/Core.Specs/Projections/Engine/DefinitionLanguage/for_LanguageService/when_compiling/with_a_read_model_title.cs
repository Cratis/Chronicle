// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class with_a_read_model_title : for_LanguageService.given.a_language_service_with_qualified_read_models
{
    void Because() => CompileExpectingErrors("projection Orders => OrderSummary\n  from OrderPlaced\n");

    [Fact] void should_reject_the_short_name() => _errors.HasErrors.ShouldBeTrue();
    [Fact] void should_suggest_the_identifier() => _errors.Errors.Single().Message.ShouldEqual($"Read model 'OrderSummary' not found; did you mean '{ReadModelName}'?");
    [Fact] void should_keep_the_error_location() => _errors.Errors.Single().Line.ShouldEqual(1);
}
