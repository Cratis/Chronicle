// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueueRouter.given;

public class a_seeded_router : a_router
{
    void Establish()
    {
        for (var queueIndex = 0; queueIndex < QueueCount; queueIndex++)
        {
            _router.Seed(queueIndex, []);
        }
    }
}
