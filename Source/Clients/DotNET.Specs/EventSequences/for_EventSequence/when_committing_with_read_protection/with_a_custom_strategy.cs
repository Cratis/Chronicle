// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_a_custom_strategy : given.a_trusted_commit
{
    void Establish() => _concurrencyScopeStrategies.GetFor(_sequence).Returns(_concurrencyScopeStrategy);

    Task Because() => Execute(async () =>
    {
        Read();
        _unit.AddEvents(EventSequenceId.Log, [new("source", "event")], []);
        await Commit();
    });

    [Fact] void should_refuse_custom_routing() => _error.ShouldBeOfExactType<ReadProtectionRequiresTheDefaultConcurrencyStrategy>();
    [Fact] void should_not_invoke_the_strategy() => _concurrencyScopeStrategy.DidNotReceive().GetScope(Arg.Any<EventSourceId>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventSourceType>(), Arg.Any<IEnumerable<EventType>>());
    [Fact] void should_not_append() => _request.ShouldBeNull();
}
