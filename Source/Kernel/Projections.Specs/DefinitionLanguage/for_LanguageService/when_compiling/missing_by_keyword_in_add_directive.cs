// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling;

public class missing_by_keyword_in_add_directive : given.a_language_service_expecting_errors
{
    const string Declaration = """
        projection Account => AccountReadModel
          from AccountCreated
            key accountId
            add balance e.amount
        """;

    void Because() => Compile(Declaration);

    [Fact] void should_have_errors() => _errors.HasErrors.ShouldBeTrue();
}
