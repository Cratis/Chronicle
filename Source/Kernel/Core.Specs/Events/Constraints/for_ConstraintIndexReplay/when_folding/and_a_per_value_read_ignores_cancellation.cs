// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_per_value_read_ignores_cancellation : given.a_replay
{
    readonly CancellationTokenSource _deadline = new();
    given.UncooperativeConstraintValues _values;
    Exception? _error;

    void Establish()
    {
        _definition = _definition with { Mode = UniqueConstraintMode.PerValue };
        Event(0, "source", "Removed");
        _values = new(_deadline);
        _unique.GetForStream(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_values);
        Start();
    }

    async Task Because() => _error = await Catch.Exception(ExecuteReplay);
    async Task ExecuteReplay() => _result = await _replay.Continue(cancellationToken: _deadline.Token).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

    void Destroy()
    {
        _values.Complete();
        _deadline.Dispose();
    }

    [Fact] void should_return_at_the_read_deadline() => _error.ShouldBeNull();
    [Fact] void should_not_complete_an_unfinished_fold() => _result.IsCompleted.ShouldBeFalse();
    [Fact] void should_not_write_partial_state() => _writes.ShouldBeEmpty();
}
