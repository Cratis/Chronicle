// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// Identifies the epoch owning an occurrence.
/// </summary>
/// <param name="OccurrenceId">The occurrence identity.</param>
/// <param name="Epoch">The fencing epoch.</param>
public record LeaseToken(
    OccurrenceId OccurrenceId,
    long Epoch);
