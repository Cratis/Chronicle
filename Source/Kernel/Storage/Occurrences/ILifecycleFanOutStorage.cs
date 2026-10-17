// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// Stores event-store-level lifecycle intents independently of namespace delivery.
/// Completed intents are retained so a retry can never take a new snapshot.
/// </summary>
public interface ILifecycleFanOutStorage
{
    /// <summary>
    /// Gets an intent by its lifecycle identity.
    /// </summary>
    /// <param name="id">The lifecycle identity.</param>
    /// <returns>The intent, if recorded.</returns>
    Task<LifecycleFanOutIntent?> Get(OccurrenceId id);

    /// <summary>
    /// Inserts an intent if absent, never replacing its snapshot or progress.
    /// </summary>
    /// <param name="intent">The initial intent.</param>
    /// <returns>Whether this call inserted it.</returns>
    Task<bool> TryInsert(LifecycleFanOutIntent intent);

    /// <summary>
    /// Reads a bounded page of unfinished intents.
    /// </summary>
    /// <param name="max">The maximum page size.</param>
    /// <returns>The unfinished intents.</returns>
    Task<IEnumerable<LifecycleFanOutIntent>> GetPending(int max);

    /// <summary>
    /// Advances progress monotonically after the corresponding namespace writes succeed.
    /// </summary>
    /// <param name="id">The lifecycle identity.</param>
    /// <param name="progress">The acknowledged prefix of the fixed namespace snapshot.</param>
    /// <returns>The durable update.</returns>
    Task Advance(OccurrenceId id, int progress);
}
