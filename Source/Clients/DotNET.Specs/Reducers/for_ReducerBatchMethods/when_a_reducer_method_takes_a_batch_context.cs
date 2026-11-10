// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reducers.for_ReducerBatchMethods;

public class when_a_reducer_method_takes_a_batch_context : given.a_registry
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => ReducerInvoker.GetEventTypesFor(_eventTypes, typeof(AccountReducer), typeof(Account)));

    [Fact] void should_refuse_the_reducer() => _error.ShouldBeOfExactType<BatchContextOnlyForBatchMethods>();
    [Fact] void should_name_the_method() => _error.Message.ShouldContain(nameof(AccountReducer.Deposit));

    public record Account(decimal Balance);

    public class AccountReducer : IReducerFor<Account>
    {
        public Account? Deposit(Deposited @event, Account? current, BatchContext context) => current;
    }
}
