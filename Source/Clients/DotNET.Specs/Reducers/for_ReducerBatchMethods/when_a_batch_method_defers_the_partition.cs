// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reducers.for_ReducerBatchMethods;

public class when_a_batch_method_defers_the_partition : given.a_registry
{
    ReduceResult _result;

    async Task Because() => _result = await new SelfReducerInvoker(_eventTypes, typeof(Account), "accounts").Invoke(
        Substitute.For<IServiceProvider>(),
        [Event(new Deposited(50), 1)],
        null,
        _context);

    [Fact] void should_not_be_a_success() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_ask_for_the_delay() => _result.RetryAfter.ShouldEqual(TimeSpan.FromSeconds(30));
    [Fact] void should_carry_no_error() => _result.ErrorMessages.ShouldBeEmpty();

    public record Account(decimal Balance)
    {
        [Observes<Deposited>]
        public static Account? Reduce(EventBatch events, Account? current)
        {
            events.Defer(events[0].Context, TimeSpan.FromSeconds(30));
            return current;
        }
    }
}
