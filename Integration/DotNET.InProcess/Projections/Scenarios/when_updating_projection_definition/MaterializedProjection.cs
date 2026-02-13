// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_updating_projection_definition;

public class MaterializedProjection : IProjectionFor<TestReadModel>
{
    public static bool MapBothProperties { get; set; }

    public void Define(IProjectionBuilderFor<TestReadModel> builder)
    {
        builder.NoAutoMap();
        if (MapBothProperties)
        {
            builder.From<TestEvent>(e => e
                .Set(m => m.Name).To(e => e.Name)
                .Set(m => m.Description).To(e => e.Description));
        }
        else
        {
            builder.From<TestEvent>(e => e
                .Set(m => m.Name).To(e => e.Name));
        }
    }
}
