// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks.for_WebhookDefinitionsStorage.given;

public class a_webhook_definitions_storage : Specification
{
    protected WebhookDefinitionsStorage _storage;
    protected IEventStoreDatabase _eventStoreDatabase;
    protected IMongoCollection<WebhookDefinition> _collection;

    void Establish()
    {
        _eventStoreDatabase = Substitute.For<IEventStoreDatabase>();
        _collection = Substitute.For<IMongoCollection<WebhookDefinition>>();

        _eventStoreDatabase.GetCollection<WebhookDefinition>(Arg.Any<string>()).Returns(_collection);
        _storage = new WebhookDefinitionsStorage(_eventStoreDatabase);
    }
}
