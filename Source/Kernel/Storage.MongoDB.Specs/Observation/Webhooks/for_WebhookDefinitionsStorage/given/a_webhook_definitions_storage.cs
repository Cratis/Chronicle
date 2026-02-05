// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Observation.Webhooks;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks.for_WebhookDefinitionsStorage.given;

public class a_webhook_definitions_storage : Specification
{
    protected WebhookDefinitionsStorage _storage;
    protected IEventStoreDatabase _eventStoreDatabase;
    protected IMongoCollection<WebhookDefinition> _collection;
    protected IWebhookSecretEncryption _encryption;

    void Establish()
    {
        _eventStoreDatabase = Substitute.For<IEventStoreDatabase>();
        _collection = Substitute.For<IMongoCollection<WebhookDefinition>>();
        _encryption = Substitute.For<IWebhookSecretEncryption>();

        // Setup encryption to pass through values for testing
        _encryption.Encrypt(Arg.Any<string>()).Returns(ci => ci.ArgAt<string>(0));
        _encryption.Decrypt(Arg.Any<string>()).Returns(ci => ci.ArgAt<string>(0));

        _eventStoreDatabase.GetCollection<WebhookDefinition>(Arg.Any<string>()).Returns(_collection);
        _storage = new WebhookDefinitionsStorage(_eventStoreDatabase, _encryption);
    }
}
