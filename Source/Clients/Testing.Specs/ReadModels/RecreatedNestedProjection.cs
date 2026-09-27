// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>Projection for the nested object lifecycle.</summary>
public class RecreatedNestedProjection : IProjectionFor<RecreatedNestedModel>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<RecreatedNestedModel> builder) => builder
        .Nested(m => m.Outer, outer => outer
            .Nested(m => m.Info, info => info
                .From<NestedRegistered>(from => from.UsingKey(e => e.Key).Set(m => m.Name).To(e => e.Name))
                .RemovedWith<NestedClearedEvent>(removed => removed.UsingKey(e => e.Key))));
}
