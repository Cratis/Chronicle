// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ReadModelReactors;

public class WatchedProjection : IProjectionFor<WatchedReadModel>
{
    public ProjectionId Identifier => "watched-projection";

    public void Define(IProjectionBuilderFor<WatchedReadModel> builder) => builder
        .From<WatchedEvent>(e => e
            .Set(m => m.Number).To(e => e.Number));
}
