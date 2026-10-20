// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts.for_ObserverHandledCountsStorage.given;

public class an_observer_handled_counts_storage : Namespaces.given.a_migrated_namespace_database
{
    protected ObserverHandledCountsStorage _storage;

    void Establish() => _storage = new(_eventStore, _namespace, _database);

    protected ObserverHandledCountsStorage FreshStorage() => new(_eventStore, _namespace, _database);
}
