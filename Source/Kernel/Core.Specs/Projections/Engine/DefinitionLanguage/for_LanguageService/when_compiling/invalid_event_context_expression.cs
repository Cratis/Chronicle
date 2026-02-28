// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.for_LanguageService.when_compiling;

public class invalid_event_context_expression : given.a_language_service_expecting_errors
{
    const string Declaration = """
        projection Account => AccountReadModel
          from AccountCreated
            key accountId
            set name to $invalidContext.name
        """;

    void Because() => Compile(Declaration);

    [Fact] void should_have_errors() => _errors.HasErrors.ShouldBeTrue();
}
