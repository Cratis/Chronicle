// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling;

public class invalid_operator_in_expression : given.a_language_service_expecting_errors
{
    const string definition = """
        projection Account => AccountReadModel
          from AccountCreated
            key accountId
            set name to e.firstName & e.lastName
        """;

    void Because() => Compile(definition);

    [Fact] void should_have_errors() => _errors.HasErrors.ShouldBeTrue();
    [Fact] void should_report_invalid_operator() => _errors.Errors.ShouldContain(e => e.Message.Contains("operator") || e.Message.Contains("&") || e.Message.Contains("expect"));
}
