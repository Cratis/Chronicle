// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Orleans.TestKit;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.given;

public class a_pii_manager : Specification
{
    protected static readonly EventStoreName EventStore = "some-event-store";
    protected static readonly EventStoreNamespaceName EventStoreNamespace = "some-namespace";
    protected static readonly EncryptionKeyIdentifier Identifier = "9c1f6a3e-7d24-4b0f-8a51-6e2d3c4b5a70";

    protected TestKitSilo _silo = new();
    protected IEncryptionKeyStorage _keyStore;
    protected IEncryptionKeyCacheClient _cacheClient;
    protected PIIManager _manager;

    async Task Establish()
    {
        _keyStore = Substitute.For<IEncryptionKeyStorage>();
        _cacheClient = Substitute.For<IEncryptionKeyCacheClient>();

        _silo.AddService(_keyStore);
        _silo.AddService(_cacheClient);

        _manager = await _silo.CreateGrainAsync<PIIManager>(Guid.Empty, new PIIManagerKey(EventStore, EventStoreNamespace));
    }
}
