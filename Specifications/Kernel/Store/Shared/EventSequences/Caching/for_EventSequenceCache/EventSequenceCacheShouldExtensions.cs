// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache;

public static class EventSequenceCacheShouldExtensions
{
    public static void ShouldHaveAllInRange(this IEnumerable<AppendedEvent> events, EventSequenceCacheRange range)
    {
        var expected = Enumerable.Range((int)range.Start.Value, (int)(range.End.Value - range.Start.Value + 1));
        var actual = events.Select(_ => (int)_.Metadata.SequenceNumber.Value);

        actual.ShouldContainOnly(expected);
    }
}
