// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.given;

public class an_event_store_subscriptions_service : Specification
{
    internal Services.Observation.EventStoreSubscriptions.EventStoreSubscriptions _service;
    protected IGrainFactory _grainFactory;
    protected IStorage _storage;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();
        _service = new Services.Observation.EventStoreSubscriptions.EventStoreSubscriptions(_grainFactory, _storage);
    }
}
