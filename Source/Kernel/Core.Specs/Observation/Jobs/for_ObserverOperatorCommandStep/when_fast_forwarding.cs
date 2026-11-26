// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverOperatorCommandStep;

public class when_fast_forwarding : given.an_observer_to_give_commands_to
{
    Exception _fastForwarded;
    Exception _alreadyAtTail;
    Exception _replaying;
    Exception _notAvailable;

    async Task Because()
    {
        _observer.FastForward().Returns(
            new FastForwardResult(FastForwardOutcome.FastForwarded, 1UL, 10UL),
            new FastForwardResult(FastForwardOutcome.AlreadyAtTail, 10UL, 10UL),
            new FastForwardResult(FastForwardOutcome.Replaying, 1UL, 1UL),
            new FastForwardResult(FastForwardOutcome.NotAvailable, 1UL, 1UL));
        _fastForwarded = await Catch.Exception(() => Give(ObserverOperatorCommand.FastForward));
        _alreadyAtTail = await Catch.Exception(() => Give(ObserverOperatorCommand.FastForward));
        _replaying = await Catch.Exception(() => Give(ObserverOperatorCommand.FastForward));
        _notAvailable = await Catch.Exception(() => Give(ObserverOperatorCommand.FastForward));
    }

    [Fact] void should_succeed_when_the_observer_was_moved() => _fastForwarded.ShouldBeNull();
    [Fact] void should_succeed_when_the_observer_was_already_at_the_tail() => _alreadyAtTail.ShouldBeNull();
    [Fact] void should_fail_when_the_observer_is_replaying() => _replaying.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_fail_when_the_observer_is_not_available() => _notAvailable.ShouldBeOfExactType<InvalidOperationException>();
}
