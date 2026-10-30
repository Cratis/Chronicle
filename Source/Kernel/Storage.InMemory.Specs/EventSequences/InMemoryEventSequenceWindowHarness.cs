// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.InMemory.Identities;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences;

/// <summary>
/// Runs the shared window contract against in-memory event sequence storage.
/// </summary>
public class InMemoryEventSequenceWindowHarness : IEventSequenceWindowHarness
{
    /// <inheritdoc/>
    public Task<IEventSequenceStorage> CreateStorage() => Task.FromResult<IEventSequenceStorage>(
        new EventSequenceStorage("window-store", EventStoreNamespaceName.Default, EventSequenceId.Log, new IdentityStorage()));

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
