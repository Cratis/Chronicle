// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reducers.for_ReducerBatchMethods;

public class when_a_batch_method_does_not_return_the_read_model : given.a_registry
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _ = new SelfReducerInvoker(_eventTypes, typeof(Account), "accounts").EventTypes);

    [Fact] void should_refuse_the_reducer() => _error.ShouldBeOfExactType<InvalidReducerBatchMethod>();

    public record Account(decimal Balance)
    {
        [Observes<Deposited>]
        public static Task Reduce(EventBatch events, Account? current) => Task.CompletedTask;
    }
}
