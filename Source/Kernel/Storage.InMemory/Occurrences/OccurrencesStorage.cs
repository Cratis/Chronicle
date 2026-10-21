// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.Occurrences;

/// <summary>
/// Stores infrastructure occurrences for one scope within this process.
/// </summary>
public partial class OccurrencesStorage
{
    readonly Dictionary<OccurrenceId, Occurrence> _occurrences = [];
    readonly object _lock = new();

    /// <summary>
    /// Inserts an ingress occurrence.
    /// </summary>
    /// <param name="occurrence">The validated ingress facts.</param>
    /// <returns>The recording outcome.</returns>
    public Task<OccurrenceRecordResult> Record(Occurrence occurrence)
    {
        lock (_lock)
        {
            if (_occurrences.TryGetValue(occurrence.Id, out var existing))
            {
                return Task.FromResult(existing.Fingerprint == occurrence.Fingerprint
                    ? OccurrenceRecordResult.AlreadyRecorded
                    : OccurrenceRecordResult.OccurrenceIdConflict);
            }

            _occurrences.Add(occurrence.Id, occurrence);
            return Task.FromResult(OccurrenceRecordResult.Recorded);
        }
    }
}
