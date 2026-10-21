// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class with_wrong_event_property_casing : for_LanguageService.given.a_language_service_with_qualified_read_models
{
    void Because() => CompileExpectingErrors("""
        projection Orders => Example.ReadModels.Order
          from OrderPlaced
            Name = name
            add Name by name
        """);

    [Fact] void should_reject_the_wrong_casing() => _errors.HasErrors.ShouldBeTrue();
    [Fact] void should_suggest_the_event_property_for_each_mapping() => _errors.Errors.Select(_ => _.Message).ShouldContainOnly("Event property 'name' not found; did you mean 'Name'?", "Event property 'name' not found; did you mean 'Name'?");
}
