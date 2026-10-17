// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Identifies the fenced lease of an occurrence attempt.
/// </summary>
/// <param name="OccurrenceId">The occurrence.</param>
/// <param name="Epoch">The claimed epoch.</param>
public record LeaseToken(OccurrenceId OccurrenceId, long Epoch);
