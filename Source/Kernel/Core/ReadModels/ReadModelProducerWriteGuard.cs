// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Security;
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
    /// <param name="provenance">The database provenance coordinator.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="restricted">Whether the incoming producer uses an unsupported source.</param>
    /// <returns>The validation task.</returns>
    internal static async Task Ensure(IReadModelSinkProvenance provenance, EventStoreName eventStore, bool restricted)
    {
        await provenance.Preserve(eventStore);
        if (restricted)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, SystemStoreAccess.Refused));
        }
    }
}
