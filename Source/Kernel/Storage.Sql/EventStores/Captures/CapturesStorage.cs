// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Storage.Captures;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Captures;

/// <summary>
/// Represents a <see cref="ICapturesStorage"/> for captures in SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class CapturesStorage(EventStoreName eventStore, IDatabase database) : ICapturesStorage, IDisposable
{
    readonly ReplaySubject<IEnumerable<Concepts.Captures.Capture>> _subject = new(1);

    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.Captures.Capture>> GetAll()
    {
        await using var scope = await database.EventStore(eventStore);
        var captures = await scope.DbContext.Captures.ToListAsync();
        return captures.Select(capture => capture.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<Concepts.Captures.Capture>> ObserveAll() => _subject;

    /// <inheritdoc/>
    public async Task<bool> Has(CaptureId id)
    {
        await using var scope = await database.EventStore(eventStore);
        var key = id.Value.ToString();
        return await scope.DbContext.Captures.AnyAsync(capture => capture.Id == key);
    }

    /// <inheritdoc/>
    public async Task<Concepts.Captures.Capture> Get(CaptureId id)
    {
        await using var scope = await database.EventStore(eventStore);
        var key = id.Value.ToString();
        var capture = await scope.DbContext.Captures.SingleAsync(capture => capture.Id == key);
        return capture.ToKernel();
    }

    /// <inheritdoc/>
    public async Task Delete(CaptureId id)
    {
        await using var scope = await database.EventStore(eventStore);
        var key = id.Value.ToString();
        var capture = await scope.DbContext.Captures.SingleOrDefaultAsync(capture => capture.Id == key);
        var observation = await scope.DbContext.CaptureObservations.SingleOrDefaultAsync(observation => observation.Id == key);
        if (observation is not null)
        {
            scope.DbContext.CaptureObservations.Remove(observation);
        }

        if (capture is not null)
        {
            scope.DbContext.Captures.Remove(capture);
        }

        if (capture is not null || observation is not null)
        {
            await scope.DbContext.SaveChangesAsync();
            await NotifyChange();
        }
    }

    /// <inheritdoc/>
    public async Task Save(Concepts.Captures.Capture capture)
    {
        await using var scope = await database.EventStore(eventStore);
        await scope.DbContext.Captures.Upsert(capture.ToSql());
        await scope.DbContext.SaveChangesAsync();
        await NotifyChange();
    }

    /// <inheritdoc/>
    public async Task<CaptureObservation> GetObservation(CaptureId id)
    {
        await using var scope = await database.EventStore(eventStore);
        var key = id.Value.ToString();
        var entry = await scope.DbContext.CaptureObservations.SingleOrDefaultAsync(observation => observation.Id == key);
        return entry?.ToKernel() ?? CaptureObservation.Empty(id);
    }

    /// <inheritdoc/>
    public async Task SaveObservation(CaptureObservation observation)
    {
        await using var scope = await database.EventStore(eventStore);
        await scope.DbContext.CaptureObservations.Upsert(observation.ToSql());
        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _subject.Dispose();
        GC.SuppressFinalize(this);
    }

    async Task NotifyChange()
    {
        var all = await GetAll();
        _subject.OnNext(all);
    }
}
