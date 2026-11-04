// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reducers.for_ReducerBatchMethods;

public class when_a_reducer_mixes_a_batch_method_and_per_event_methods : given.a_registry
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _ = new SelfReducerInvoker(_eventTypes, typeof(Account), "accounts").EventTypes);

    [Fact] void should_refuse_the_reducer() => _error.ShouldBeOfExactType<ReducerMixesBatchAndPerEventMethods>();

    public record Account(decimal Balance)
    {
        public static Account? Reduce(Deposited @event, Account? current) => current;

        [Observes<Withdrawn>]
        public static Account? Reduce(EventBatch events, Account? current) => current;
    }
}
