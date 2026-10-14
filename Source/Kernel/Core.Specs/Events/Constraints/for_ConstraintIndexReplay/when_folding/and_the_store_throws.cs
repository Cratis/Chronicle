// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_the_store_throws : given.a_replay
{
    void Establish()
    {
        Event(0, "first", "Claimed", "first");
        Event(1, "second", "Claimed", "second");
        _unique.Configure().Save(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new IOException("write unavailable")));
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_keep_the_constraint_pending() => _result.IsCompleted.ShouldBeFalse();
    [Fact] void should_report_the_failed_constraint() => _result.Failed.Keys.Select(name => name.Value).ShouldContainOnly("unique");
    [Fact] void should_attempt_only_one_claim_for_the_failed_constraint() => _unique.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(_unique.Save)).ShouldEqual(1);
}
