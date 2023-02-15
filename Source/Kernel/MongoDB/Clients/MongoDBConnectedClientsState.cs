// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Aksio.Cratis.Clients;
using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.Grains.Clients;
using Aksio.Cratis.MongoDB;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Clients;

/// <summary>
/// Represents an implementation of <see cref="IConnectedClientsState"/> state for MongoDB.
/// </summary>
public class MongoDBConnectedClientsState : IConnectedClientsState
{
    readonly ProviderFor<ISharedDatabase> _sharedDatabaseProvider;

    IMongoCollection<MongoDBConnectedClientsForMicroserviceState> Collection => _sharedDatabaseProvider().GetCollection<MongoDBConnectedClientsForMicroserviceState>(CollectionNames.ConnectedClients);

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBConnectedClientsState"/> class.
    /// </summary>
    /// <param name="sharedDatabaseProvider">Provider for <see cref="ISharedDatabase"/>.</param>
    public MongoDBConnectedClientsState(ProviderFor<ISharedDatabase> sharedDatabaseProvider)
    {
        _sharedDatabaseProvider = sharedDatabaseProvider;
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<ConnectedClient>> GetAllForMicroservice(MicroserviceId microserviceId)
    {
        var clients = Collection.Find(_ => _.Id == microserviceId).SingleOrDefault()?.Clients ?? Array.Empty<ConnectedClient>();
        var observable = new BehaviorSubject<IEnumerable<ConnectedClient>>(clients);
        var cursor = Collection.Watch();
        _ = Task.Run(async () =>
        {
            try
            {
                while (await cursor.MoveNextAsync())
                {
                    if (observable.IsDisposed)
                    {
                        cursor.Dispose();
                        return;
                    }

                    if (!cursor.Current.Any()) continue;
                    var clients = await Collection.FindAsync(_ => _.Id == microserviceId);
                    if (!observable.IsDisposed)
                    {
                        observable.OnNext(clients.SingleOrDefault()?.Clients ?? Array.Empty<ConnectedClient>());
                    }
                }
            }
            catch (Exception)
            {
            }
        });

        return observable;
    }
}
