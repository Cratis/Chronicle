// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Registrations;
using Cratis.Chronicle.Storage.Observation.Registrations;
using Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Registrations;

/// <summary>
/// Persists complete registrations and independently versioned settings at event-store scope.
/// </summary>
/// <param name="open">Opens an event-store database context for each operation.</param>
/// <param name="serializerOptions">Options for serializing complete definitions.</param>
public class ObserverRegistrationsStorage(Func<Task<DbContextScope<EventStoreDbContext>>> open, JsonSerializerOptions serializerOptions) : IObserverRegistrationsStorage
{
    /// <inheritdoc/>
    public Task<ObserverRegistration?> Get(ObserverId observerId, EventSequenceId eventSequenceId) => Read<ObserverRegistration>(Identity(observerId, eventSequenceId));

    /// <inheritdoc/>
    public Task<IEnumerable<ObserverRegistration>> GetActive() => Find(null);

    /// <inheritdoc/>
    public Task<IEnumerable<ObserverRegistration>> GetByOwner(ProgramIdentifier owner) => Find(StorageIdentity.Hash(owner.Value));

    /// <inheritdoc/>
    public Task<bool> TrySave(ObserverRegistration registration, ObserverRegistrationRevision expectedRevision)
    {
        if (!IsNext(registration.Revision.Value, expectedRevision.Value))
        {
            return Task.FromResult(false);
        }

        var row = Entry(Identity(registration.ObserverId, registration.EventSequenceId), "registration", registration.Revision.Value, registration);
        row.Owner = StorageIdentity.Hash(registration.Owner.Value);
        row.IsActive = registration.IsActive;
        return Write(row, expectedRevision.Value);
    }

    /// <inheritdoc/>
    public async Task<bool> TryDelete(ObserverId observerId, EventSequenceId eventSequenceId, ObserverRegistrationRevision expectedRevision)
    {
        await using var scope = await open();
        var id = Identity(observerId, eventSequenceId);
        var revision = Number(expectedRevision.Value);
        return await scope.DbContext.Set<ObserverRegistrationEntry>().Where(row => row.Id == id && row.Revision == revision).ExecuteDeleteAsync() == 1;
    }

    /// <inheritdoc/>
    public Task<ObserverGenerationFloor?> GetGenerationFloor() => Read<ObserverGenerationFloor>("floor");

    /// <inheritdoc/>
    public Task<bool> TrySaveGenerationFloor(ObserverGenerationFloor floor, ulong expectedVersion)
    {
        if (!IsNext(floor.Version, expectedVersion))
        {
            return Task.FromResult(false);
        }

        var row = Entry("floor", "floor", floor.Version, floor);
        row.Generation = Number(floor.Generation);
        return Write(row, expectedVersion, row.Generation);
    }

    /// <inheritdoc/>
    public Task<ObserverOwnerSettings?> GetOwnerSettings(ProgramIdentifier owner) => Read<ObserverOwnerSettings>($"owner:{StorageIdentity.Hash(owner.Value)}");

    /// <inheritdoc/>
    public Task<bool> TrySaveOwnerSettings(ObserverOwnerSettings settings, ulong expectedRevision) =>
        settings.HasValidTimeout && IsNext(settings.Revision, expectedRevision)
            ? Write(Entry($"owner:{StorageIdentity.Hash(settings.Owner.Value)}", "owner", settings.Revision, settings), expectedRevision)
            : Task.FromResult(false);

    static bool IsNext(ulong revision, ulong expected) => expected < ulong.MaxValue && revision == expected + 1;

    static string Number(ulong number) => number.ToString("D20", CultureInfo.InvariantCulture);

    static string Identity(ObserverId observerId, EventSequenceId eventSequenceId) => $"registration:{StorageIdentity.Hash(JsonSerializer.Serialize(new[] { observerId.Value, eventSequenceId.Value }))}";

    ObserverRegistrationEntry Entry<T>(string id, string kind, ulong revision, T value) => new()
    {
        Id = id,
        Kind = kind,
        Revision = Number(revision),
        Generation = Number(0),
        Data = JsonSerializer.Serialize(value, serializerOptions)
    };

    async Task<T?> Read<T>(string id)
    {
        await using var scope = await open();
        var row = await scope.DbContext.Set<ObserverRegistrationEntry>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == id);
        return row is null ? default : JsonSerializer.Deserialize<T>(row.Data, serializerOptions);
    }

    async Task<IEnumerable<ObserverRegistration>> Find(string? owner)
    {
        await using var scope = await open();
        var rows = scope.DbContext.Set<ObserverRegistrationEntry>().AsNoTracking().Where(row => row.Kind == "registration");
        rows = owner is null ? rows.Where(row => row.IsActive) : rows.Where(row => row.Owner == owner);
        var data = await rows.Select(row => row.Data).ToListAsync();
        return data.Select(value => JsonSerializer.Deserialize<ObserverRegistration>(value, serializerOptions)!).ToArray();
    }

    async Task<bool> Write(ObserverRegistrationEntry entry, ulong expectedRevision, string? maximumGeneration = null)
    {
        await using var scope = await open();
        if (expectedRevision > 0)
        {
            var revision = Number(expectedRevision);
            var rows = scope.DbContext.Set<ObserverRegistrationEntry>().Where(row => row.Id == entry.Id && row.Revision == revision);
            if (maximumGeneration is not null)
            {
                rows = rows.Where(row => row.Generation.CompareTo(maximumGeneration) <= 0);
            }

            return await rows.ExecuteUpdateAsync(set => set
                .SetProperty(row => row.Data, entry.Data)
                .SetProperty(row => row.Revision, entry.Revision)
                .SetProperty(row => row.Generation, entry.Generation)
                .SetProperty(row => row.Owner, entry.Owner)
                .SetProperty(row => row.IsActive, entry.IsActive)) == 1;
        }

        try
        {
            scope.DbContext.Set<ObserverRegistrationEntry>().Add(entry);
            await scope.DbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception exception) when (StorageWriteConflicts.IsUnique(exception))
        {
            return false;
        }
    }
}
