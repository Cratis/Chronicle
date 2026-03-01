// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.for_LanguageService.when_compiling;

public class missing_arrow_operator : given.a_language_service_expecting_errors
{
    const string Declaration = """
        projection Account AccountReadModel
          from AccountCreated
            key accountId
        """;

    void Because() => Compile(Declaration);

    [Fact] void should_have_errors() => _errors.HasErrors.ShouldBeTrue();
    [Fact] void should_report_expected_arrow() => _errors.Errors.ShouldContain(e => e.Message.Contains("=>") || e.Message.Contains("Arrow"));
}
