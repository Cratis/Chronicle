// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage.Sinks;
using Grpc.Core;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Orders database eligibility before producer validation and materialized access.
/// </summary>
public static class ReadModelPublicReadGuard
{
    /// <summary>
    /// Preserves retained evidence, checks the physical database, then checks the producer.
    /// </summary>
    /// <param name="provenance">The database provenance coordinator.</param>
    /// <param name="grains">The grain factory.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The actual read namespace.</param>
    /// <param name="definition">The requested definition.</param>
    /// <returns>The admission task.</returns>
    /// <exception cref="RpcException">The sink does not support public read-model reads.</exception>
    public static async Task Ensure(
        IReadModelSinkProvenance provenance,
        IGrainFactory grains,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        ReadModelDefinition definition)
    {
        if (!SinkProvenanceExemptions.AllowsPublicReads(definition.Sink.Type))
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, SystemStoreAccess.Refused));
        }

        await provenance.EnsureReadable(eventStore, @namespace, definition);
        await ReadModelReadPolicy.EnsureReadable(grains, eventStore, definition);
    }
}
