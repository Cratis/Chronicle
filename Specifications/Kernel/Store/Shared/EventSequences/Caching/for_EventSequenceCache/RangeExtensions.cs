// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache;

public static class RangeExtensions
{
    public static IEnumerable<AppendedEvent> GenerateEvents(this IEnumerable<int> range, EventSequenceNumber sequenceNumberOffset) =>
        range.Select(_ =>
            new AppendedEvent(
                new(sequenceNumberOffset + (ulong)_, new(Guid.Empty, EventGeneration.First)),
                new EventContext(
                    EventSourceId.Unspecified,
                    DateTimeOffset.Now,
                    DateTimeOffset.MinValue,
                    TenantId.Development,
                    CorrelationId.New(),
                    CausationId.System,
                    CausedBy.System), new JsonObject())).ToArray();
}
