// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling;

public class missing_closing_brace_in_expression : given.a_language_service_expecting_errors
{
    const string Definition = """
        projection Account => AccountReadModel
          from AccountCreated
            key accountId
            set name to e.name.value
        """;

    void Because() => Compile(Definition);

    [Fact] void should_have_errors() => _errors.HasErrors.ShouldBeTrue();
}
