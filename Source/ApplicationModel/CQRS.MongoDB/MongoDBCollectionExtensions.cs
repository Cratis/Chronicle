// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using MongoDB.Driver;

namespace Aksio.Cratis.Applications.Queries.MongoDB;

/// <summary>
/// Extension methods for <see cref="IMongoCollection{T}"/>.
/// </summary>
public static class MongoDBCollectionExtensions
{
    /// <summary>
    /// Find a single document based on Id.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
    /// <param name="id">Id of document.</param>
    /// <typeparam name="T">Type of document.</typeparam>
    /// <typeparam name="TId">Type of identifier.</typeparam>
    /// <returns>The document if found, default if not.</returns>
    public static T? FindById<T, TId>(this IMongoCollection<T> collection, TId id) =>
        collection.Find(Builders<T>.Filter.Eq(new StringFieldDefinition<T, TId>("_id"), id)).SingleOrDefault();

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
        return await collection.Observe<TDocument, IEnumerable<TDocument>>(() => collection.FindAsync(filter, options), (cursor, observable) =>
            observable.OnNext(cursor.ToList()));
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
        return await collection.Observe<TDocument, IEnumerable<TDocument>>(() => collection.FindAsync(filter, options), (cursor, observable) =>
            observable.OnNext(cursor.ToList()));
    }

    /// <summary>
    /// Create an observable query that will observe the collection for changes matching the filter criteria.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
    /// <param name="filter">Optional filter.</param>
    /// <param name="options">Optional options.</param>
    /// <typeparam name="TDocument">Type of document in the collection.</typeparam>
    /// <returns>Async Task holding <see cref="ClientObservable{T}"/> with a collection of the type for the collection.</returns>
    public static async Task<ClientObservable<TDocument>> ObserveSingle<TDocument>(
        this IMongoCollection<TDocument> collection,
        Expression<Func<TDocument, bool>>? filter,
        FindOptions<TDocument, TDocument>? options = null)
    {
        filter ??= _ => true;
        return await collection.ObserveSingle(() => collection.FindAsync(filter, options));
    }

    /// <summary>
    /// Create an observable query that will observe the collection for changes matching the filter criteria.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
    /// <param name="filter">Optional filter.</param>
    /// <param name="options">Optional options.</param>
    /// <typeparam name="TDocument">Type of document in the collection.</typeparam>
    /// <returns>Async Task holding <see cref="ClientObservable{T}"/> with a collection of the type for the collection.</returns>
    public static async Task<ClientObservable<TDocument>> ObserveSingle<TDocument>(
        this IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument>? filter = null,
        FindOptions<TDocument, TDocument>? options = null)
    {
        filter ??= FilterDefinition<TDocument>.Empty;
        return await collection.ObserveSingle(() => collection.FindAsync(filter, options));
    }

    /// <summary>
    /// Create an observable query that will observe a single document based on Id of the document in the collection for changes matching the filter criteria.
    /// </summary>
    /// <param name="collection"><see cref="IMongoCollection{T}"/> to extend.</param>
    /// <param name="id">The identifier of the document to observe.</param>
    /// <typeparam name="TDocument">Type of document in the collection.</typeparam>
    /// <typeparam name="TId">Type of id - key.</typeparam>
    /// <returns>Async Task holding <see cref="ClientObservable{T}"/> with an instance of the type.</returns>
    public static async Task<ClientObservable<TDocument>> ObserveById<TDocument, TId>(this IMongoCollection<TDocument> collection, TId id)
    {
        var filter = Builders<TDocument>.Filter.Eq(new StringFieldDefinition<TDocument, TId>("_id"), id);
        return await collection.ObserveSingle(() => collection.FindAsync(filter));
    }

    static async Task<ClientObservable<TDocument>> ObserveSingle<TDocument>(
         this IMongoCollection<TDocument> collection,
         Func<Task<IAsyncCursor<TDocument>>> findCall)
    {
        return await collection.Observe<TDocument, TDocument>(findCall, (cursor, observable) =>
        {
            var result = cursor.FirstOrDefault();
            if (result is not null)
            {
                observable.OnNext(result);
            }
        });
    }

    static async Task<ClientObservable<TResult>> Observe<TDocument, TResult>(
         this IMongoCollection<TDocument> collection,
         Func<Task<IAsyncCursor<TDocument>>> findCall,
         Action<IAsyncCursor<TDocument>, ClientObservable<TResult>> onNext)
    {
        var observable = new ClientObservable<TResult>();
        var response = await findCall();
        onNext(response, observable);

        var cursor = collection.Watch();

        _ = Task.Run(async () =>
        {
            try
            {
                while (await cursor.MoveNextAsync())
                {
                    if (!cursor.Current.Any()) continue;
                    var response = await findCall();
                    onNext(response, observable);
                }
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Cursor disposed.");
            }
        });

        observable.ClientDisconnected = () => cursor.Dispose();

        return observable;
    }
}
