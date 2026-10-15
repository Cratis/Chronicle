// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Preserves the fenced transition outcome across generated transports.
/// </summary>
/// <param name="Status">Updated, AlreadyCompleted or OccurrenceLeaseLost.</param>
public record OccurrenceTransitionResult(OccurrenceLeaseResult Status);
