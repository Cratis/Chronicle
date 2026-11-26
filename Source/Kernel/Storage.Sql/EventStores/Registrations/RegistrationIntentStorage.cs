// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Registrations;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Registrations;

/// <summary>
/// Stores registration intents in the event-store database, one row per kind and key.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class RegistrationIntentStorage(EventStoreName eventStore, IDatabase database) : IRegistrationIntentStorage
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<RegistrationIntent>> GetAll()
    {
        await using var scope = await database.EventStore(eventStore);
        var rows = await scope.DbContext.RegistrationIntents.AsNoTracking().ToListAsync();
        return [.. rows.Select(ToIntent)];
    }

    /// <inheritdoc/>
    public async Task Save(RegistrationIntent intent)
    {
        await using var scope = await database.EventStore(eventStore);
        await scope.DbContext.RegistrationIntents.Upsert(ToEntry(intent));
    }

    /// <summary>
    /// Gets the identifier of the row of an intent.
    /// </summary>
    /// <param name="kind">The <see cref="RegistrationKind"/>.</param>
    /// <param name="key">The key the intent is coalesced under.</param>
    /// <returns>The identifier.</returns>
    internal static string IdFor(RegistrationKind kind, string key) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{(int)kind}:{key}")));

    static RegistrationIntentEntry ToEntry(RegistrationIntent intent) => new()
    {
        Id = IdFor(intent.Kind, intent.Key),
        Kind = (int)intent.Kind,
        Key = intent.Key,
        Registrant = intent.Registrant,
        Revision = intent.Revision,
        Hash = intent.Hash,
        State = (int)intent.State,
        Payload = intent.Payload.ToArray(),
        FullSetOwner = intent.FullSetOwner,
        Version = intent.Version,
        Attempts = intent.Attempts,
        Failure = intent.Failure,
        UpdatedAt = intent.UpdatedAt
    };

    static RegistrationIntent ToIntent(RegistrationIntentEntry row) => new(
        (RegistrationKind)row.Kind,
        row.Key,
        row.Registrant,
        row.Revision,
        row.Hash,
        (RegistrationIntentState)row.State,
        row.Payload,
        row.FullSetOwner,
        row.Version,
        row.Attempts,
        row.Failure,
        row.UpdatedAt);
}
