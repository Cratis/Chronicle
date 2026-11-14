// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline.given;

public class a_pipeline_over_a_sink : Specification
{
    protected static readonly AppliedEventMarkerJoin _join = new("def", "type", "customerId");
    protected IProjection _projection;
    protected EventStoreNamespaceName _namespace = "namespace";
    protected Key _partition = (Key)"some-partition";

    protected ProjectionPipeline CreatePipeline(ISink sink, params AppliedEventMarkerJoin[] joins)
    {
        _projection = Substitute.For<IProjection>();
        _projection.MarkedJoins.Returns(joins);
        return new ProjectionPipeline(
            _projection,
            sink,
            Substitute.For<IChangesetStorage>(),
            Substitute.For<IObjectComparer>(),
            [],
            new ProjectionHandleLock(),
            Substitute.For<IReplayScopedCache>(),
            NullLogger<ProjectionPipeline>.Instance);
    }
}
