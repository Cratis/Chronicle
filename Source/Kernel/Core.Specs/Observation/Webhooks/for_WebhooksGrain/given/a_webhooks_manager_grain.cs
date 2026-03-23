// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Namespaces;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhooksGrain.given;

public class a_webhooks_manager_grain : Specification
{
    protected Webhooks _grain;
    protected TestKitSilo _silo;
    protected IStorage<WebhooksState> _stateStorage;
    protected INamespaces _namespacesGrain;
    protected IWebhook _webhookGrain;

    async Task Establish()
    {
        _silo = new TestKitSilo();

        var localSiloDetails = Substitute.For<ILocalSiloDetails>();
        _silo.AddService(localSiloDetails);

        _namespacesGrain = Substitute.For<INamespaces>();
        _namespacesGrain.GetAll().Returns([]);
        _silo.AddProbe(_ => _namespacesGrain);

        _webhookGrain = Substitute.For<IWebhook>();
        _silo.AddProbe(_ => _webhookGrain);

        _grain = await _silo.CreateGrainAsync<Webhooks>("test-event-store");
        _stateStorage = _silo.StorageManager.GetStorage<WebhooksState>(typeof(Webhooks).FullName);
    }
}
