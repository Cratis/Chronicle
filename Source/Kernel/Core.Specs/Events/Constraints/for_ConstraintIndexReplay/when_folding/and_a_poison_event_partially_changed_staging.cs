// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Concepts.Events.Constraints;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_poison_event_partially_changed_staging : given.a_replay
{
    async Task Establish()
    {
        _definition = _definition with
        {
            Mode = UniqueConstraintMode.PerValue,
            RemovedWith = ["RemovedOne", "Removed"],
            RemovalEventDefinitions = [new("RemovedOne", ["email"])]
        };
        await _claims.Save("source", _definition, 0, Hash("removed"));
        Event(0, "source", "Claimed", "removed");
        Event(1, "source", "RemovedOne", "removed");
        Event(2, "source", "Removed");
        _unique.Configure().GetForStream("source", _definition, string.Empty, Arg.Any<CancellationToken>())
            .Returns(call => BrokenValues(call.ArgAt<CancellationToken>(3)));
        Start();
    }

    async IAsyncEnumerable<UniqueConstraintValue> BrokenValues([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var value in await _claims.GetFor("source", _definition, cancellationToken: cancellationToken)) yield return value;
        throw new FormatException("unreadable claim row after a valid row");
    }

    async Task Because()
    {
        await _replay.Continue();
        _replay.SettlePoison(_definition.Name, 2);
        _result = await _replay.Continue();
    }

    [Fact] void should_complete_after_settling_the_poison_event() => _result.IsCompleted.ShouldBeTrue();
    [Fact] async Task should_preserve_the_preceding_valid_release() => (await Values("source")).ShouldBeEmpty();
}
