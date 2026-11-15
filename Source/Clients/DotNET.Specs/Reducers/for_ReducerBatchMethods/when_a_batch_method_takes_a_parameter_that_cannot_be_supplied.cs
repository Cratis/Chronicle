// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reducers.for_ReducerBatchMethods;

public class when_a_batch_method_takes_a_parameter_that_cannot_be_supplied : given.a_registry
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => ReducerInvoker.GetEventTypesFor(_eventTypes, typeof(AccountReducer), typeof(Account)));

    [Fact] void should_refuse_the_reducer() => _error.ShouldBeOfExactType<BatchMethodParameterCannotBeSupplied>();
    [Fact] void should_name_the_parameter() => _error.Message.ShouldContain("currency");

    public record Account(decimal Balance);

    public class AccountReducer : IReducerFor<Account>
    {
        [Observes<Deposited>]
        public Account? Reduce(EventBatch events, Account? current, string currency) => current;
    }
}
