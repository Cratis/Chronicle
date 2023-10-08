// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Aksio.Cratis.Connections;
using Aksio.Cratis.Kernel.Grains.Clients;
using Aksio.Cratis.MongoDB;
using Aksio.DependencyInversion;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Clients;

/// <summary>
/// Represents an implementation of <see cref="IConnectedClientsState"/> state for MongoDB.
/// </summary>
public class MongoDBConnectedClientsState : IConnectedClientsState
{
    readonly ProviderFor<IClusterDatabase> _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBConnectedClientsState"/> class.
    /// </summary>
    /// <param name="database">Provider for <see cref="ISharedDatabase"/>.</param>
    public MongoDBConnectedClientsState(ProviderFor<IClusterDatabase> database)
    {
        _database = database;
    }

    IMongoCollection<ConnectedClientsState> Collection => _database().GetCollection<ConnectedClientsState>(CollectionNames.ConnectedClients);

    /// <inheritdoc/>
    public IObservable<IEnumerable<ConnectedClient>> GetAll()
    {
        var clients = GetClients(Collection.Find(_ => _.Id == 0).SingleOrDefault());
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
                    var clients = await Collection.FindAsync(_ => _.Id == 0);
                    if (!observable.IsDisposed)
                    {
                        observable.OnNext(GetClients(clients.SingleOrDefault()));
                    }
                }
            }
            catch (Exception)
            {
                observable.OnNext(Array.Empty<ConnectedClient>());
            }
        });

        return observable;
    }

    IEnumerable<ConnectedClient> GetClients(ConnectedClientsState state) => state?.Clients.OrderBy(_ => _.ConnectionId).ToArray().AsEnumerable() ?? Array.Empty<ConnectedClient>();
}
