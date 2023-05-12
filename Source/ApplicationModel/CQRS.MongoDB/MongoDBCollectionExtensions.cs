// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Concepts;
using MongoDB.Bson;
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
        FilterDefinition<TDocument>? filter = null,
        FindOptions<TDocument, TDocument>? options = null)
    {
        filter ??= FilterDefinition<TDocument>.Empty;
        return await collection.Observe<TDocument, IEnumerable<TDocument>>(
            () => collection.FindAsync(filter, options),
            (documents, observable) => observable.OnNext(documents));
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
        return await collection.Observe<TDocument, TDocument>(
            findCall,
            (documents, observable) =>
            {
                var result = documents.FirstOrDefault();
                if (result is not null)
                {
                    observable.OnNext(result);
                }
            });
    }

    static async Task<ClientObservable<TResult>> Observe<TDocument, TResult>(
        this IMongoCollection<TDocument> collection,
        Func<Task<IAsyncCursor<TDocument>>> findCall,
        Action<IEnumerable<TDocument>, ClientObservable<TResult>> onNext)
    {
        var observable = new ClientObservable<TResult>();
        var response = await findCall();
        var documents = response.ToList();
        onNext(documents, observable);
        response.Dispose();
        response = null!;

        var options = new ChangeStreamOptions
        {
            FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
        };

        // Todo: Get the filter definition in as argument and then add it to the pipeline
        var filter = Builders<ChangeStreamDocument<TDocument>>.Filter.In(
            new StringFieldDefinition<ChangeStreamDocument<TDocument>, string>("operationType"),
            new[] { "insert", "replace", "update" });

        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<TDocument>>().Match(filter);

        var cursor = await collection.WatchAsync(pipeline, options);
        var idProperty = typeof(TDocument).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public)!;

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

                    foreach (var changeDocument in cursor.Current)
                    {
                        if (changeDocument.DocumentKey.TryGetValue("_id", out var idValue))
                        {
                            var id = BsonTypeMapper.MapToDotNetValue(idValue);
                            if (idProperty.PropertyType.IsConcept())
                            {
                                id = ConceptFactory.CreateConceptInstance(idProperty.PropertyType, id);
                            }
                            var document = documents.Find(_ => idProperty.GetValue(_)!.Equals(id));
                            if (document is not null)
                            {
                                var index = documents.IndexOf(document);
                                documents[index] = changeDocument.FullDocument;
                            }
                            else
                            {
                                documents.Add(changeDocument.FullDocument);
                            }
                        }
                    }

                    onNext(documents, observable);
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
