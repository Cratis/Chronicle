// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using MongoDB.Driver;

namespace Aksio.Cratis.Applications.Queries.MongoDB
{
    /// <summary>
    /// Extension methods for <see cref="IMongoCollection{T}"/>.
    /// </summary>
    public static class MongoDBCollectionExtensions
    {
        /// <summary>
        /// Create an observable query that will observe the collection for changes matching the filter criteria.
        /// </summary>
        /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
        /// <param name="filter">Optional filter.</param>
        /// <param name="options">Optional options.</param>
        /// <typeparam name="TDocument">Type of document in the collection.</typeparam>
        /// <returns>Async Task holding <see cref="ClientObservable{T}"/> with a collection of the type for the collection.</returns>
        public static async Task<ClientObservable<IEnumerable<TDocument>>> Observe<TDocument>(
            this IMongoCollection<TDocument> collection,
            Expression<Func<TDocument, bool>>? filter,
            FindOptions<TDocument, TDocument>? options = null)
        {
            filter ??= _ => true;
            return await collection.Observe(() => collection.FindAsync(filter, options));
        }

        /// <summary>
        /// Create an observable query that will observe the collection for changes matching the filter criteria.
        /// </summary>
        /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
        /// <param name="filter">Optional filter.</param>
        /// <param name="options">Optional options.</param>
        /// <typeparam name="TDocument">Type of document in the collection.</typeparam>
        /// <returns>Async Task holding <see cref="ClientObservable{T}"/> with a collection of the type for the collection.</returns>
        public static async Task<ClientObservable<IEnumerable<TDocument>>> Observe<TDocument>(
            this IMongoCollection<TDocument> collection,
            FilterDefinition<TDocument>? filter = null,
            FindOptions<TDocument, TDocument>? options = null)
        {
            filter ??= FilterDefinition<TDocument>.Empty;
            return await collection.Observe(() => collection.FindAsync(filter, options));
        }

        static async Task<ClientObservable<IEnumerable<TDocument>>> Observe<TDocument>(
             this IMongoCollection<TDocument> collection,
             Func<Task<IAsyncCursor<TDocument>>> findCall)
        {
            var observable = new ClientObservable<IEnumerable<TDocument>>();
            var response = await findCall();
            observable.OnNext(response.ToList());
            var cursor = collection.Watch();

            _ = Task.Run(async () =>
            {
                try
                {
                    while (await cursor.MoveNextAsync())
                    {
                        if (!cursor.Current.Any()) continue;
                        var response = await findCall();
                        observable.OnNext(response.ToList());
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Cursor disposed.")
                }
            });

            observable.ClientDisconnected = () => cursor.Dispose();

            return observable;
        }
    }
}
