// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor.given;

public class an_empty_cache : all_dependencies
{
    void Establish() =>
        cache.Setup(_ => _.GetView(IsAny<EventSequenceNumber>(), IsAny<EventSequenceNumber>())).Returns(new SortedSet<AppendedEvent>());
}
