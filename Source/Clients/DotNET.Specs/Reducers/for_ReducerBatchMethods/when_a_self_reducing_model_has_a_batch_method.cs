// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reducers.for_ReducerBatchMethods;

public class when_a_self_reducing_model_has_a_batch_method : given.a_registry
{
    SelfReducerInvoker _invoker;
    ReduceResult _result;

    void Establish() => _invoker = new(_eventTypes, typeof(Account), "accounts");

    async Task Because() => _result = await _invoker.Invoke(
        Substitute.For<IServiceProvider>(),
        [Event(new Deposited(50), 1), Event(new Withdrawn(20), 2)],
        new Account(100, ObservationOrigin.Unknown),
        _context);

    [Fact] void should_be_discovered_as_a_self_reducing_model() => SelfReducerMethods.HasSelfShape(typeof(Account)).ShouldBeTrue();
    [Fact] void should_subscribe_to_the_observed_event_types() => _invoker.EventTypes.Select(_ => _.Id.Value).ShouldContainOnly("deposited", "withdrawn");
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_fold_the_batch_in_one_call() => ((Account)_result.ReadModelState!).Balance.ShouldEqual(130m);
    [Fact] void should_pass_the_context() => ((Account)_result.ReadModelState!).Origin.ShouldEqual(ObservationOrigin.CatchUp);
    [Fact] void should_have_handled_up_to_the_last_event() => _result.LastSuccessfullyObservedEvent.ShouldEqual((EventSequenceNumber)2UL);

    public record Account(decimal Balance, ObservationOrigin Origin)
    {
        [Observes<Deposited>]
        [Observes<Withdrawn>]
        public static Account? Reduce(EventBatch events, Account? current, BatchContext context) =>
            events.Aggregate(current ?? new(0, context.Origin), (account, e) => e.Event switch
            {
                Deposited deposited => account with { Balance = account.Balance + deposited.Amount, Origin = context.Origin },
                Withdrawn withdrawn => account with { Balance = account.Balance - withdrawn.Amount, Origin = context.Origin },
                _ => account
            });
    }
}
