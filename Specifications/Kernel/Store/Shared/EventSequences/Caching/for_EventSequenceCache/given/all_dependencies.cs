// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching.for_EventSequenceCache.given;

public class all_dependencies : Specification
{
    protected Mock<IEventSequenceStorageProvider> storage_provider;

    void Establish()
    {
        storage_provider = new();
    }
}
