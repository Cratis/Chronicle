// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// Derives opaque identities using the publication identity's length-prefixed SHA-256 encoding.
/// </summary>
public static class OccurrenceIdentities
{
    /// <summary>
    /// Derives an ingress identity without delimiter ambiguity.
    /// </summary>
    /// <param name="trigger">The trigger identity.</param>
    /// <param name="idempotencyId">The caller's retry identity.</param>
    /// <returns>The scoped occurrence identity.</returns>
    public static OccurrenceId ForIngress(TriggerId trigger, string idempotencyId) => new(Hash(trigger.Value, idempotencyId));

    /// <summary>
    /// Derives a scheduled identity independent of the definition revision.
    /// </summary>
    /// <param name="token">The schedule token.</param>
    /// <param name="due">The due epoch second.</param>
    /// <returns>The scoped occurrence identity.</returns>
    public static OccurrenceId ForSchedule(ScheduleToken token, long due) => new(Hash(token.Value, due.ToString(CultureInfo.InvariantCulture)));

    /// <summary>
    /// Derives the first-writer-wins receipt identity.
    /// </summary>
    /// <param name="occurrence">The occurrence identity.</param>
    /// <param name="effectKey">The nonempty effect name chosen by the handler.</param>
    /// <returns>The namespace-scoped receipt identity.</returns>
    public static string ForEffect(OccurrenceId occurrence, string effectKey) => $"effect:{Hash(occurrence.Value, effectKey)}";

    static string Hash(params string[] parts)
    {
        var text = new StringBuilder();
        foreach (var part in parts)
        {
            text.Append(part.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(part).Append('|');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
    }
}
