// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class with_no_matching_read_model : for_LanguageService.given.a_language_service_with_qualified_read_models
{
    void Because() => CompileExpectingErrors("projection Orders => Unknown\n  from OrderPlaced\n");

    [Fact] void should_keep_the_original_error() => _errors.Errors.Single().Message.ShouldEqual("Read model 'Unknown' not found");
}
