// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents an <see cref="IEventSequenceMutationDigestKeys"/> that has no keys, used when storage is built without a key
/// source. Every mutation operation then fails closed.
/// </summary>
public sealed class UnavailableEventSequenceMutationDigestKeys : IEventSequenceMutationDigestKeys
{
    /// <summary>
    /// Gets the shared instance.
    /// </summary>
    public static readonly UnavailableEventSequenceMutationDigestKeys Instance = new();

    UnavailableEventSequenceMutationDigestKeys()
    {
    }

    /// <inheritdoc/>
    public Task<EventSequenceMutationDigestKey?> TryGetFor(EventStoreName eventStore) => Task.FromResult<EventSequenceMutationDigestKey?>(null);
}
