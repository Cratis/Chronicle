// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerDispatcher.when_dispatching_a_page;

public class and_a_partition_is_unchanged : given.a_reducer_dispatcher
{
    readonly List<object?> _initialStates = [];

    void Establish() => _handler.OnNext(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<object?>(), Arg.Any<IServiceProvider>()).Returns(call =>
    {
        _initialStates.Add(call.Arg<object?>());
        return new ReduceResult(new State("state-" + _initialStates.Count), 7, [], string.Empty);
    });

    async Task Because()
    {
        await _dispatcher.Dispatch(_handler, PageWith(("a", null, [6UL])), "ns", Publish);

        var page = PageWith(("a", null, [7UL]), ("b", null, [8UL]));
        page.Partitions[0].InitialStateUnchanged = true;
        await _dispatcher.Dispatch(_handler, page, "ns", Publish);
    }

    [Fact] void should_succeed_for_the_unchanged_partition() => _published[1].Partitions.Single(_ => _.Partition == "a").State.ShouldEqual(ObservationState.Success);
    [Fact] void should_succeed_for_the_other_partition() => _published[1].Partitions.Single(_ => _.Partition == "b").State.ShouldEqual(ObservationState.Success);
    [Fact] void should_have_reduced_the_unchanged_partition_from_its_cached_state() => _initialStates.ShouldContain(new State("state-1"));
}
