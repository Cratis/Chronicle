// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling;

public class arithmetic_operations : given.a_language_service
{
    const string definition = """
        projection Account => AccountReadModel
          from MoneyDeposited
            key accountId
            add Balance by amount
          from MoneyWithdrawn
            key accountId
            subtract Balance by amount
        """;

    ProjectionDefinition _result;

    void Because() => _result = Compile(definition);

    [Fact] void should_have_two_from_definitions() => _result.From.Count.ShouldEqual(2);
    [Fact] void should_have_money_deposited_event() => _result.From.ContainsKey((EventType)"MoneyDeposited").ShouldBeTrue();
    [Fact] void should_have_money_withdrawn_event() => _result.From.ContainsKey((EventType)"MoneyWithdrawn").ShouldBeTrue();
    [Fact] void should_have_add_operation_in_deposit() => _result.From[(EventType)"MoneyDeposited"].Properties[new PropertyPath("Balance")].ShouldContain("+=");
    [Fact] void should_have_subtract_operation_in_withdraw() => _result.From[(EventType)"MoneyWithdrawn"].Properties[new PropertyPath("Balance")].ShouldContain("-=");
}
