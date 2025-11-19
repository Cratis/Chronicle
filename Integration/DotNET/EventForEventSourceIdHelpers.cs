// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Integration;

public static class EventForEventSourceIdHelpers
{
    public static EventForEventSourceId Create(object content, EventSourceId? eventSourceId = null, Auditing.Causation? causation = null)
    {
        return new EventForEventSourceId(
            eventSourceId ?? $"Random event source {Random.Shared.Next()}",
            content,
            causation ?? new Auditing.Causation(DateTimeOffset.UtcNow, Auditing.CausationType.Unknown, new Dictionary<string, string>()));
    }

    public static IEnumerable<EventForEventSourceId> CreateMultiple(Func<int, object> content, int num, EventSourceId? eventSourceId = null)
        => Enumerable.Range(0, num).Select(i => Create(content(i), eventSourceId));
}
