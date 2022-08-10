// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache.when_creating;

public class and_there_are_no_events_in_sequence : given.no_events_in_sequence
{
    [Fact] void should_have_an_empty_range() => cache.CurrentRange.ShouldEqual(new(0, 0));
}
