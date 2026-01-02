// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling;

public class missing_by_keyword_in_add_directive : given.a_language_service_expecting_errors
{
    const string Definition = """
        projection Account => AccountReadModel
          from AccountCreated
            key accountId
            add balance e.amount
        """;

    void Because() => Compile(Definition);

    [Fact] void should_have_errors() => _errors.HasErrors.ShouldBeTrue();
    [Fact] void should_report_missing_by() => _errors.Errors.ShouldContain(e => e.Message.Contains("by") || e.Message.Contains("expect"));
}
