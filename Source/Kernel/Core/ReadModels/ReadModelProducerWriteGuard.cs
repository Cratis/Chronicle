// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;
using Grpc.Core;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Refuses new protected producers outside the system store, including direct internal storage-provider writes.
/// </summary>
internal static class ReadModelProducerWriteGuard
{
    /// <summary>
    /// Preserves existing evidence before validating a replacement. Refusal never overwrites that evidence.
    /// </summary>
    /// <param name="storage">The storage.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="sequence">The incoming producer's event sequence.</param>
    /// <returns>The validation task.</returns>
    internal static async Task Ensure(IStorage storage, EventStoreName eventStore, EventSequenceId sequence)
    {
        await ReadModelProvenance.For(storage, eventStore).Preserve();
        if (!SystemStoreAccess.IsSystem(eventStore) && sequence.IsSystemSequence())
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, SystemStoreAccess.Refused));
        }
    }
}
