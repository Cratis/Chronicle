// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reducers.for_ReducerBatchMethods;

public class when_a_reducer_implementing_reducer_for_has_a_batch_method : given.a_registry
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

    [Fact] void should_subscribe_to_the_observed_event_type() => _invoker.EventTypes.Select(_ => _.Id.Value).ShouldContainOnly("deposited");
    [Fact] void should_fold_the_batch() => ((Account)_result.ReadModelState!).Balance.ShouldEqual(75m);

    public record Account(decimal Balance);

    public class AccountReducer : IReducerFor<Account>
    {
        [Observes<Deposited>]
        public Task<Account?> Reduce(EventBatch events, Account? current) =>
            Task.FromResult<Account?>(new(events.Sum(_ => ((Deposited)_.Event).Amount) + (current?.Balance ?? 0)));
    }
}
