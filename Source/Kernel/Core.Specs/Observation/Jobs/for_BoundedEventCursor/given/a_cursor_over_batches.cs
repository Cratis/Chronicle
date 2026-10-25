// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Observation.Jobs.for_BoundedEventCursor.given;

public abstract class a_cursor_over_batches : Specification
{
    protected IEventCursor _inner;
    protected IEventCursor _cursor;
    protected int _innerMoveNextCalls;

    protected abstract int Bound { get; }

    protected abstract IReadOnlyList<AppendedEvent[]> Batches { get; }

    protected static AppendedEvent[] Events(params ulong[] sequenceNumbers) =>
        [.. sequenceNumbers.Select(_ => AppendedEvent.EmptyWithEventSequenceNumber(_))];

    protected static string Joined(ulong[] numbers) => string.Join(", ", numbers);

    protected static ulong[] Numbers(IEnumerable<AppendedEvent> events) =>
        [.. events.Select(_ => _.Context.SequenceNumber.Value)];

    void Establish()
    {
        var batches = Batches;
        var index = -1;
        _inner = Substitute.For<IEventCursor>();
        _inner.MoveNext().Returns(_ =>
        {
            _innerMoveNextCalls++;
            index++;
            return Task.FromResult(index < batches.Count);
        });
        _inner.Current.Returns(_ => index >= 0 && index < batches.Count ? batches[index] : []);
        _cursor = new BoundedEventCursor(_inner, Bound);
    }
}
