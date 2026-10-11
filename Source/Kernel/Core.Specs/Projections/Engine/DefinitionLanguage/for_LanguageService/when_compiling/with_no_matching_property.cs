// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class with_no_matching_property : for_LanguageService.given.a_language_service_with_qualified_read_models
{
    void Because() => CompileExpectingErrors("""
        projection Orders => Example.ReadModels.Order
          from OrderPlaced
            Unknown = Name
            Name = unknown
        """);

    [Fact] void should_keep_the_target_error() => _errors.Errors.Select(_ => _.Message).ShouldContain("Read model property 'Unknown' not found");
    [Fact] void should_keep_the_source_error() => _errors.Errors.Select(_ => _.Message).ShouldContain("Event property 'unknown' not found");
}
