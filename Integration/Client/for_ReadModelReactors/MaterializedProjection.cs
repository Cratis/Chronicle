// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ReadModelReactors;

public class MaterializedProjection : IProjectionFor<MaterializedReadModel>
{
    public ProjectionId Identifier => "materialized-reconnect-projection";

    public void Define(IProjectionBuilderFor<MaterializedReadModel> builder) => builder
        .From<WatchedEvent>(e => e
            .Set(m => m.Id).ToEventSourceId()
            .Set(m => m.Number).To(e => e.Number));
}
