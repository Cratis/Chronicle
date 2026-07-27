// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueueRouter.given;

public class a_seeded_single_queue_router : a_seeded_router
{
    protected override int QueueCount => 1;
}
