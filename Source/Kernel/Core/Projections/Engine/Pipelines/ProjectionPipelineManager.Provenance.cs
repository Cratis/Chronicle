// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Security;
using Grpc.Core;
using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

public partial class ProjectionPipelineManager
{
    async Task EnsureProvenance(EventStoreName eventStore, EventStoreNamespaceName sinkNamespace, EngineProjection projection)
    {
        await provenance.EnsureReadable(eventStore, sinkNamespace, projection.ReadModel);
        if (!projection.EventSequenceId.IsSystemSequence()) return;

        // Runtime projections expose the sequence, not the producer owner; missing owner evidence cannot authorize this work.
        var definitions = storage.GetEventStore(eventStore).Projections;
        if (!await definitions.Has(projection.Identifier) || !(await definitions.Get(projection.Identifier)).IsKernelOwned)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, SystemStoreAccess.Refused));
        }
    }
}
