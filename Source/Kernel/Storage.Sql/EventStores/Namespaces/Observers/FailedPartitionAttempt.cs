// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers;

/// <summary>
/// Represents a failed partition attempt for SQL storage.
/// </summary>
/// <param name="Occurred">When the attempt occurred.</param>
/// <param name="SequenceNumber">The sequence number of the event.</param>
/// <param name="Messages">Collection of error messages.</param>
/// <param name="StackTrace">Stack trace of the error.</param>
public record FailedPartitionAttempt(
    DateTimeOffset Occurred,
    ulong SequenceNumber,
    IEnumerable<string> Messages,
    string StackTrace);
