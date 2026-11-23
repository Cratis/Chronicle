// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Security;
using Grpc.Core;
using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

public partial class ProjectionPipelineManager
{
    async Task EnsureProvenance(EventStoreName eventStore, EventStoreNamespaceName sinkNamespace, EngineProjection projection)
    {
        if (projection.Graph is not null) return;
        await provenance.EnsureReadable(eventStore, sinkNamespace, projection.ReadModel);
        if (projection.Definition.UsesUnsupportedEventSequence())
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, SystemStoreAccess.Refused));
        }
    }
}
