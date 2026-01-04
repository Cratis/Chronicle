// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class arithmetic_operations : given.a_language_service_with_schemas<given.AccountReadModel>
{
    const string Definition = """
        projection Account => AccountReadModel
          from MoneyDeposited
            key accountId
            add balance by amount
          from MoneyWithdrawn
            key accountId
            subtract balance by amount
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.MoneyDeposited), typeof(given.MoneyWithdrawn)];

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(Definition);

    [Fact] void should_have_two_from_definitions() => _result.From.Count.ShouldEqual(2);
    [Fact] void should_have_money_deposited_event() => _result.From.ContainsKey((EventType)"MoneyDeposited").ShouldBeTrue();
    [Fact] void should_have_money_withdrawn_event() => _result.From.ContainsKey((EventType)"MoneyWithdrawn").ShouldBeTrue();
    [Fact] void should_have_add_operation_in_deposit() => _result.From[(EventType)"MoneyDeposited"].Properties[new PropertyPath("balance")].ShouldContain("+=");
    [Fact] void should_have_subtract_operation_in_withdraw() => _result.From[(EventType)"MoneyWithdrawn"].Properties[new PropertyPath("balance")].ShouldContain("-=");
}
