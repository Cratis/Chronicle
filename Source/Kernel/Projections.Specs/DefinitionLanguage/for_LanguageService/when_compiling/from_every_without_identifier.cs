// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling;

public class from_every_without_identifier : given.a_language_service_expecting_errors
{
    const string definition = """
        projection Account => AccountReadModel
          from every
            key accountId
        """;

    void Because() => Compile(definition);

    [Fact] void should_have_errors() => _errors.HasErrors.ShouldBeTrue();
    [Fact] void should_report_missing_identifier() => _errors.Errors.ShouldContain(e => e.Message.Contains("identifier") || e.Message.Contains("event") || e.Message.Contains("expect"));
}
