// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class with_wrong_read_model_property_casing : for_LanguageService.given.a_language_service_with_qualified_read_models
{
    void Because() => CompileExpectingErrors("""
        projection Orders => Example.ReadModels.Order
          from OrderPlaced
            name = Name
            clear details.name
          children items identified by Name
            from OrderPlaced
              Name = Name
        """);

    [Fact] void should_reject_the_wrong_casing() => _errors.HasErrors.ShouldBeTrue();
    [Fact] void should_suggest_the_mapping_property() => _errors.Errors.Select(_ => _.Message).ShouldContain("Read model property 'name' not found; did you mean 'Name'?");
    [Fact] void should_suggest_the_full_nested_path() => _errors.Errors.Select(_ => _.Message).ShouldContain("Read model property 'details.name' not found; did you mean 'Details.Name'?");
    [Fact] void should_suggest_the_children_property() => _errors.Errors.Select(_ => _.Message).ShouldContain("Read model property 'items' not found; did you mean 'Items'?");
}
