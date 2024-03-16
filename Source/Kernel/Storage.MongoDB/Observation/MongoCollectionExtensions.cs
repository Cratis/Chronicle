// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using MongoDB.Driver;

namespace Cratis.Kernel.Storage.MongoDB.Observation;

/// <summary>
/// Extensions for working with MongoDB collections.
/// </summary>
public static class MongoCollectionExtensions
{
    static readonly string[] _mongoDBOperations = ["insert", "replace", "update", "delete"];

    /// <summary>
    /// Observe a collection for changes.
    /// </summary>
    /// <param name="collection">Collection to observe.</param>
    /// <param name="initialItems">The initial items.</param>
    /// <param name="handleChanges">Action to handle changes.</param>
    /// <param name="filter">Optional filter.</param>
    /// <typeparam name="TResult">Type of the items in the result.</typeparam>
    /// <typeparam name="TDocument">Type of document for the collection.</typeparam>
    /// <returns>An observable for the items.</returns>
    public static IObservable<IEnumerable<TResult>> Observe<TResult, TDocument>(
        this IMongoCollection<TDocument> collection,
        IEnumerable<TResult> initialItems,
        Action<IChangeStreamCursor<ChangeStreamDocument<TDocument>>, List<TResult>> handleChanges,
        FilterDefinition<ChangeStreamDocument<TDocument>>? filter = null)
    {
        var items = new List<TResult>(initialItems);
        var observable = new BehaviorSubject<IEnumerable<TResult>>(initialItems);
        var operationTypeFilter = Builders<ChangeStreamDocument<TDocument>>.Filter.In(
            new StringFieldDefinition<ChangeStreamDocument<TDocument>, string>("operationType"),
            _mongoDBOperations);

        var actualFilter = filter is not null ?
                            Builders<ChangeStreamDocument<TDocument>>.Filter.And(operationTypeFilter, filter) :
                            operationTypeFilter;

        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<TDocument>>().Match(actualFilter);

        _ = Task.Run(async () =>
        {
            try
            {
                var cursor = await collection.WatchAsync(
                    pipeline,
                    new ChangeStreamOptions { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup });

                while (await cursor.MoveNextAsync())
                {
                    if (observable.IsDisposed)
                    {
                        cursor.Dispose();
                        return;
                    }

                    if (!cursor.Current.Any()) continue;
                    if (!observable.IsDisposed)
                    {
                        handleChanges(cursor, items);
                        observable.OnNext(items);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Watch failed: " + ex.Message);
            }
        });

        return observable;
    }
}
