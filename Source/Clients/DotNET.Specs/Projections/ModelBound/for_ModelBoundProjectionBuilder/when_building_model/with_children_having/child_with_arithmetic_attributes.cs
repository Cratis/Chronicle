// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_having;

public class child_with_arithmetic_attributes : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(AccountOpened),
            typeof(DepositMade),
            typeof(WithdrawalMade),
            typeof(TransactionRecorded),
            typeof(InterestAdded)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(Bank));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact]
    void should_have_children_for_accounts()
    {
        _result.Children.Keys.ShouldContain(nameof(Bank.Accounts));
    }

    [Fact]
    void should_have_from_definition_for_deposit_made()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DepositMade)).ToContract();
        var childrenDef = _result.Children[nameof(Bank.Accounts)];
        childrenDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_from_definition_for_withdrawal_made()
    {
        var eventType = event_types.GetEventTypeFor(typeof(WithdrawalMade)).ToContract();
        var childrenDef = _result.Children[nameof(Bank.Accounts)];
        childrenDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_from_definition_for_transaction_recorded()
    {
        var eventType = event_types.GetEventTypeFor(typeof(TransactionRecorded)).ToContract();
        var childrenDef = _result.Children[nameof(Bank.Accounts)];
        childrenDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_have_from_definition_for_interest_added()
    {
        var eventType = event_types.GetEventTypeFor(typeof(InterestAdded)).ToContract();
        var childrenDef = _result.Children[nameof(Bank.Accounts)];
        childrenDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_add_balance_from_deposit_made()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DepositMade)).ToContract();
        var childrenDef = _result.Children[nameof(Bank.Accounts)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        var expression = fromDef.Properties[nameof(Account.Balance)];
        expression.ShouldContain(WellKnownExpressions.Add);
        expression.ShouldContain(nameof(DepositMade.Amount));
    }

    [Fact]
    void should_subtract_balance_from_withdrawal_made()
    {
        var eventType = event_types.GetEventTypeFor(typeof(WithdrawalMade)).ToContract();
        var childrenDef = _result.Children[nameof(Bank.Accounts)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        var expression = fromDef.Properties[nameof(Account.Balance)];
        expression.ShouldContain(WellKnownExpressions.Subtract);
        expression.ShouldContain(nameof(WithdrawalMade.Amount));
    }

    [Fact]
    void should_increment_transaction_count_from_transaction_recorded()
    {
        var eventType = event_types.GetEventTypeFor(typeof(TransactionRecorded)).ToContract();
        var childrenDef = _result.Children[nameof(Bank.Accounts)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        var expression = fromDef.Properties[nameof(Account.TransactionCount)];
        expression.ShouldEqual($"{WellKnownExpressions.Increment}()");
    }

    [Fact]
    void should_count_total_deposits()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DepositMade)).ToContract();
        var childrenDef = _result.Children[nameof(Bank.Accounts)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        var expression = fromDef.Properties[nameof(Account.TotalDeposits)];
        expression.ShouldEqual($"{WellKnownExpressions.Count}()");
    }

    [Fact]
    void should_add_interest_total_from_interest_added()
    {
        var eventType = event_types.GetEventTypeFor(typeof(InterestAdded)).ToContract();
        var childrenDef = _result.Children[nameof(Bank.Accounts)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        var expression = fromDef.Properties[nameof(Account.TotalInterest)];
        expression.ShouldContain(WellKnownExpressions.Add);
        expression.ShouldContain(nameof(InterestAdded.InterestAmount));
    }
}

[EventType]
public record AccountOpened(AccountId Id, AccountName Name);

[EventType]
public record DepositMade(decimal Amount);

[EventType]
public record WithdrawalMade(decimal Amount);

[EventType]
public record TransactionRecorded();

[EventType]
public record InterestAdded(decimal InterestAmount);

public record BankId(Guid Value);
public record AccountId(Guid Value);
public record AccountName(string Value);

public record Account(
    AccountId Id,
    AccountName Name,

    [AddFrom<DepositMade>(nameof(DepositMade.Amount))]
    [SubtractFrom<WithdrawalMade>(nameof(WithdrawalMade.Amount))]
    decimal Balance,

    [Increment<TransactionRecorded>]
    int TransactionCount,

    [Count<DepositMade>]
    int TotalDeposits,

    [AddFrom<InterestAdded>(nameof(InterestAdded.InterestAmount))]
    decimal TotalInterest);

[FromEvent<BankCreated>]
public record Bank(
    BankId Id,

    [ChildrenFrom<AccountOpened>(identifiedBy: nameof(Account.Id))]
    IEnumerable<Account> Accounts);

[EventType]
public record BankCreated(BankId Id);
