// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reducers.for_ReducerBatchMethods;

public class when_a_batch_reducer_has_a_helper_taking_a_batch_context : given.a_registry
{
    ReducerInvoker _invoker;
    ReduceResult _result;

    void Establish()
    {
        var activator = Substitute.For<IClientArtifactsActivator>();
        activator.Activate(Arg.Any<IServiceProvider>(), typeof(AccountReducer)).Returns(_ =>
            new ActivatedArtifact(new AccountReducer(), typeof(AccountReducer), Substitute.For<ILogger<ActivatedArtifact>>()));
        _invoker = new(_eventTypes, activator, typeof(AccountReducer), typeof(Account), "accounts");
    }

    async Task Because() => _result = await _invoker.Invoke(
        Substitute.For<IServiceProvider>(),
        [Event(new Deposited(50), 1), Event(new Deposited(25), 2)],
        null,
        _context);

    [Fact] void should_accept_the_reducer() => _invoker.EventTypes.Select(_ => _.Id.Value).ShouldContainOnly("deposited");
    [Fact] void should_fold_the_batch_through_the_helper() => ((Account)_result.ReadModelState!).Balance.ShouldEqual(75m);

    public record Account(decimal Balance);

    public class AccountReducer : IReducerFor<Account>
    {
        [Observes<Deposited>]
        public Account? Reduce(EventBatch events, Account? current, BatchContext context)
        {
            foreach (var @event in events)
            {
                current = Apply((Deposited)@event.Event, current, context);
            }

            return current;
        }

        Account? Apply(Deposited @event, Account? current, BatchContext context) => new(@event.Amount + (current?.Balance ?? 0));
    }
}
