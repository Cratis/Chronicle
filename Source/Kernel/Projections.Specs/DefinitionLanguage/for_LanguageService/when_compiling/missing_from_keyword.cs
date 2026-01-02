// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling;

public class missing_from_keyword : given.a_language_service_expecting_errors
{
    const string definition = """
        projection Account => AccountReadModel
          AccountCreated
            key accountId
        """;

    void Because() => Compile(definition);

    [Fact] void should_have_errors() => _errors.HasErrors.ShouldBeTrue();
    [Fact] void should_report_unexpected_identifier() => _errors.Errors.ShouldContain(e => e.Message.Contains("from") || e.Message.Contains("directive"));
}
