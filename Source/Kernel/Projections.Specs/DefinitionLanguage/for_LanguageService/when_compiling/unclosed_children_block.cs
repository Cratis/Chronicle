// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling;

public class unclosed_children_block : given.a_language_service_expecting_errors
{
    const string Declaration = """
        projection Account => AccountReadModel
          from AccountCreated
            key accountId
            children transactions
              from TransactionAdded
                key transactionId
        """;

    void Because() => Compile(Declaration);

    [Fact] void should_have_errors() => _errors.HasErrors.ShouldBeTrue();
    [Fact] void should_report_missing_child_property() => _errors.Errors.ShouldContain(e => e.Message.Contains("as") || e.Message.Contains("identifier") || e.Message.Contains("expect"));
}
