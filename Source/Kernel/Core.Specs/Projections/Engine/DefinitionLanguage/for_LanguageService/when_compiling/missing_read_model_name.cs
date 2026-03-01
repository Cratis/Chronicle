// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.for_LanguageService.when_compiling;

public class missing_read_model_name : given.a_language_service_expecting_errors
{
    const string Declaration = """
        projection Account =>
          from AccountCreated
            key accountId
        """;

    void Because() => Compile(Declaration);

    [Fact] void should_have_errors() => _errors.HasErrors.ShouldBeTrue();
}
