// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Grains.Observation.Webhooks.for_Webhooks.given;

public class a_webhooks_service_grain : Specification
{
    internal Services.Observation.Webhooks.Webhooks _webhooks;
    protected IGrainFactory _grainFactory;
    protected IStorage _storage;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();
        _webhooks = new Services.Observation.Webhooks.Webhooks(_grainFactory, _storage);
    }
}
