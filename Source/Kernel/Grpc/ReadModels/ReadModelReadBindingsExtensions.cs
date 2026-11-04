// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.ReadModels;
using Grpc.Core;

namespace Cratis.Chronicle.Services.ReadModels;

/// <summary>
/// Extension methods for <see cref="IReadModelReadBindings"/> at the gRPC boundary.
/// </summary>
static class ReadModelReadBindingsExtensions
{
    /// <summary>
    /// Resolve a read binding, reporting an occurrence that does not belong to the read model as an invalid argument.
    /// </summary>
    /// <param name="readBindings">The <see cref="IReadModelReadBindings"/> to resolve with.</param>
    /// <param name="eventStore">The event store to read.</param>
    /// <param name="eventStoreNamespace">The namespace to read.</param>
    /// <param name="definition">The read model definition.</param>
    /// <param name="occurrence">An explicit occurrence, or null for the primary container.</param>
    /// <returns>The <see cref="ReadModelReadBinding"/> for the read.</returns>
    /// <exception cref="RpcException">Thrown with <see cref="StatusCode.InvalidArgument"/> when the occurrence is unknown.</exception>
    internal static async Task<ReadModelReadBinding> ResolveOrReject(
        this IReadModelReadBindings readBindings,
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        ReadModelDefinition definition,
        ReadModelContainerName? occurrence)
    {
        try
        {
            return await readBindings.Resolve(eventStore, eventStoreNamespace, definition, occurrence);
        }
        catch (UnknownReadModelOccurrence exception)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, exception.Message));
        }
    }
}
