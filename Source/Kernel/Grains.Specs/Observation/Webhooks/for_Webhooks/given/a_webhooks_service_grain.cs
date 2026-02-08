// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Security;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Grains.Observation.Webhooks.for_Webhooks.given;

public class a_webhooks_service_grain : Specification
{
    internal Services.Observation.Webhooks.Webhooks _webhooks;
    protected IGrainFactory _grainFactory;
    protected IStorage _storage;
    protected IWebhookDefinitionComparer _webhookDefinitionComparer;
    protected IEncryption _encryption;
    protected IOAuthClient _oauthClient;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();
        _webhookDefinitionComparer = Substitute.For<IWebhookDefinitionComparer>();
        _encryption = Substitute.For<IEncryption>();
        _oauthClient = Substitute.For<IOAuthClient>();
        _webhooks = new Services.Observation.Webhooks.Webhooks(_grainFactory, _storage, _webhookDefinitionComparer, _encryption, _oauthClient);
    }
}
