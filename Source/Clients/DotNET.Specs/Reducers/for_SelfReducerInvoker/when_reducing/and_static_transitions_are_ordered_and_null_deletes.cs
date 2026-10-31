// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_SelfReducerInvoker.when_reducing;

public class and_static_transitions_are_ordered_and_null_deletes : Specification
{
    ReduceResult _result;

    async Task Because()
    {
        var registry = Substitute.For<IEventTypes>();
        registry.AllClrTypes.Returns(ImmutableList.Create(typeof(Credit)));
        registry.GetEventTypeFor(typeof(Credit)).Returns(new EventType("credit", 1));
        var invoker = new SelfReducerInvoker(registry, typeof(Balance), "balance");
        var inputs = new[] { 3, 4, -1, 2 }.Select((amount, index) => new EventAndContext(new Credit(amount), EventContext.Empty with { SequenceNumber = (ulong)index }));
        _result = await invoker.Invoke(Substitute.For<IServiceProvider>(), inputs, null);
    }

    [Fact] void should_replace_null_state_after_deletion() => ((Balance)_result.ReadModelState!).Amount.ShouldEqual(2);
    [Fact] void should_report_the_last_successful_position() => _result.LastSuccessfullyObservedEvent.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_report_success() => _result.IsSuccess.ShouldBeTrue();

    public record Credit(int Amount);

    public record Balance(int Amount)
    {
        public static Balance? Reduce(Credit @event, Balance? current) => @event.Amount == -1 ? null : new((current?.Amount ?? 0) + @event.Amount);
    }
}
