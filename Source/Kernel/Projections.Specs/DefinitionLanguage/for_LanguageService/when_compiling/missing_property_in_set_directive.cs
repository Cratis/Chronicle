// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling;

public class missing_property_in_set_directive : given.a_language_service_expecting_errors
{
    const string Definition = """
        projection Account => AccountReadModel
          from AccountCreated
            key accountId
            set to e.name
        """;

    void Because() => Compile(Definition);

    [Fact] void should_have_errors() => _errors.HasErrors.ShouldBeTrue();
    [Fact] void should_report_missing_property() => _errors.Errors.ShouldContain(e => e.Message.Contains("property") || e.Message.Contains("identifier") || e.Message.Contains("expect"));
}
