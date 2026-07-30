// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Storage.Captures;
using Cratis.Reactive;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Captures;

/// <summary>
/// Represents a <see cref="ICapturesStorage"/> for captures in MongoDB.
/// </summary>
/// <param name="eventStoreDatabase">The <see cref="IEventStoreDatabase"/>.</param>
public class CapturesStorage(
    IEventStoreDatabase eventStoreDatabase) : ICapturesStorage
{
    IMongoCollection<Capture> Collection => eventStoreDatabase.GetCollection<Capture>(WellKnownCollectionNames.Captures);

    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.Captures.Capture>> GetAll()
    {
        using var result = await Collection.FindAsync(FilterDefinition<Capture>.Empty);
        var captures = await result.ToListAsync();
        return captures.Select(capture => capture.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<Concepts.Captures.Capture>> ObserveAll() =>
        new TransformingSubject<IEnumerable<Capture>, IEnumerable<Concepts.Captures.Capture>>(
            Collection.Observe(),
            captures => captures.Select(capture => capture.ToKernel()));

    /// <inheritdoc/>
    public Task<bool> Has(CaptureId id) =>
        Collection.Find(capture => capture.Id == id).AnyAsync();

    /// <inheritdoc/>
    public async Task<Concepts.Captures.Capture> Get(CaptureId id)
    {
        using var result = await Collection.FindAsync(capture => capture.Id == id);
        return (await result.SingleAsync()).ToKernel();
    }

    /// <inheritdoc/>
    public Task Delete(CaptureId id) =>
        Collection.DeleteOneAsync(capture => capture.Id == id);

    /// <inheritdoc/>
    public Task Save(Concepts.Captures.Capture capture) =>
        Collection.ReplaceOneAsync(
            filter: c => c.Id == capture.Id,
            replacement: capture.ToMongoDB(),
            options: new ReplaceOptions { IsUpsert = true });
}
