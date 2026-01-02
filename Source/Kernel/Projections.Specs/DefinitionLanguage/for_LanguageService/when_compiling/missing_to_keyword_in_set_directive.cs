// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling;

public class missing_to_keyword_in_set_directive : given.a_language_service_expecting_errors
{
    const string definition = """
        projection Account => AccountReadModel
          from AccountCreated
            key accountId
            set name e.name
        """;

    void Because() => Compile(definition);

    [Fact] void should_have_errors() => _errors.HasErrors.ShouldBeTrue();
    [Fact] void should_report_missing_to() => _errors.Errors.ShouldContain(e => e.Message.Contains("to") || e.Message.Contains("expect"));
}
