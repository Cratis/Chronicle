// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Occurrences;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Occurrences;

/// <summary>
/// Maps immutable ingress and queryable fencing facts to infrastructure documents.
/// </summary>
internal static class OccurrenceDocuments
{
    /// <summary>
    /// Builds an occurrence document.
    /// </summary>
    /// <param name="occurrence">The durable facts.</param>
    /// <returns>The document.</returns>
    public static BsonDocument From(Occurrence occurrence) => new()
    {
        { "_id", occurrence.Id.Value }, { "trigger", occurrence.Trigger.Value },
        { "data", JsonSerializer.Serialize(occurrence) }, { "state", (int)occurrence.State },
        { "epoch", occurrence.Epoch }, { "attempts", occurrence.Attempts },
        { "owner", occurrence.Owner is null ? BsonNull.Value : new BsonString(occurrence.Owner) },
        { "expiry", Ticks(occurrence.LeaseExpiry) }, { "nextAttempt", Ticks(occurrence.NextAttempt) },
        { "lastError", occurrence.LastError is null ? BsonNull.Value : new BsonString(occurrence.LastError) }
    };

    /// <summary>
    /// Reconstructs current delivery state independently of the ingress JSON.
    /// </summary>
    /// <param name="document">The stored document.</param>
    /// <returns>The occurrence.</returns>
    public static Occurrence ToOccurrence(BsonDocument document) => JsonSerializer.Deserialize<Occurrence>(document["data"].AsString)! with
    {
        State = (OccurrenceState)document["state"].AsInt32,
        Epoch = document["epoch"].ToInt64(),
        Attempts = document["attempts"].AsInt32,
        Owner = Text(document["owner"]),
        LeaseExpiry = Instant(document["expiry"]),
        NextAttempt = Instant(document["nextAttempt"]),
        LastError = Text(document["lastError"])
    };

    /// <summary>
    /// Stores exact UTC ticks without BSON datetime's millisecond truncation.
    /// </summary>
    /// <param name="value">The instant, if any.</param>
    /// <returns>The tick value or BSON null.</returns>
    public static BsonValue Ticks(DateTimeOffset? value) => value.HasValue ? new BsonInt64(value.Value.UtcTicks) : BsonNull.Value;

    static DateTimeOffset? Instant(BsonValue value) => value.IsBsonNull ? null : new DateTimeOffset(value.ToInt64(), TimeSpan.Zero);
    static string? Text(BsonValue value) => value.IsBsonNull ? null : value.AsString;
}
