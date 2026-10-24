// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers;

public static class ParentHistorySubstitutes
{
    public static void WithParentHistory(this IEventSequenceStorage storage, EventSourceId source, params AppendedEvent[] events) =>
        storage.GetInstancesOfAnyInRange(
            Arg.Is<IEnumerable<EventSourceId>>(ids => ids.Contains(source)),
            Arg.Any<IEnumerable<EventTypeId>>(),
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<CancellationToken>())
        .Returns(call =>
        {
            var from = call.ArgAt<EventSequenceNumber>(2);
            var upTo = call.ArgAt<EventSequenceNumber>(3);
            var types = call.ArgAt<IEnumerable<EventTypeId>>(1);
            var matches = events.Where(e => e.Context.SequenceNumber <= upTo && types.Contains(e.Context.EventType.Id)).OrderBy(e => e.Context.SequenceNumber).ToArray();
            var predecessor = matches.LastOrDefault(e => e.Context.SequenceNumber < from);
            IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> result = matches.Length == 0
                ? new Dictionary<EventSourceId, IReadOnlyList<AppendedEvent>>()
                : new Dictionary<EventSourceId, IReadOnlyList<AppendedEvent>> { [source] = matches.Where(e => e == predecessor || e.Context.SequenceNumber >= from).ToArray() };
            return Task.FromResult(result);
        });
}
