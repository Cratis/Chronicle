// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.for_LanguageService.when_compiling;

public class duplicate_key_directive : given.a_language_service_expecting_errors
{
    const string Declaration = """
        projection Account => AccountReadModel
          from AccountCreated
            key accountId
            key customerId
        """;

    void Because() => Compile(Declaration);

    [Fact] void should_have_errors() => _errors.HasErrors.ShouldBeTrue();
    [Fact] void should_report_duplicate_key() => _errors.Errors.ShouldContain(e => e.Message.Contains("key") || e.Message.Contains("duplicate") || e.Message.Contains("already"));
}
