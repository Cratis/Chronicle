// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reducers.for_ReducerBatchMethods;

public class when_a_batch_method_takes_an_optional_cancellation_token : given.a_registry
{
    CancellationTokenSource _cancellation;
    AccountReducer _reducer;
    ReducerInvoker _invoker;
    ReduceResult _result;

    void Establish()
    {
        _cancellation = new();
        _context = new("accounts", "store", "ns", EventSequenceId.Log, ObservationOrigin.CatchUp, [new EventSourceId("account")], 2, 1UL, 2UL, null, _cancellation.Token);
        _reducer = new();
        var activator = Substitute.For<IClientArtifactsActivator>();
        activator.Activate(Arg.Any<IServiceProvider>(), typeof(AccountReducer)).Returns(_ =>
            new ActivatedArtifact(_reducer, typeof(AccountReducer), Substitute.For<ILogger<ActivatedArtifact>>()));
        _invoker = new(_eventTypes, activator, typeof(AccountReducer), typeof(Account), "accounts");
    }

    void Destroy() => _cancellation.Dispose();

    async Task Because() => _result = await _invoker.Invoke(Substitute.For<IServiceProvider>(), [Event(new Deposited(50), 1)], null, _context);

    [Fact] void should_fold_the_batch() => ((Account)_result.ReadModelState!).Balance.ShouldEqual(50m);
    [Fact] void should_pass_the_cancellation_token_of_the_page() => _reducer.CancellationToken.ShouldEqual(_cancellation.Token);

    public record Account(decimal Balance);

    public class AccountReducer : IReducerFor<Account>
    {
        public CancellationToken CancellationToken { get; private set; }

        [Observes<Deposited>]
        public Account? Reduce(EventBatch events, Account? current, CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;
            return new(events.Sum(_ => ((Deposited)_.Event).Amount) + (current?.Balance ?? 0));
        }
    }
}
