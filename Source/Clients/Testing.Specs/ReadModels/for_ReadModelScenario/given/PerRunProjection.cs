// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

public class PerRunProjection : IProjectionFor<PerRunReadModel>
{
    public void Define(IProjectionBuilderFor<PerRunReadModel> builder) => builder
        .From<PerRunRecorded>(from => from.Set(model => model.Second).To(e => e.Value));
}
