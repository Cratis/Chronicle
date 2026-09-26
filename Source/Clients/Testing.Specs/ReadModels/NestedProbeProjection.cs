// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>Projects registered names and clears the nested info on rename.</summary>
public class NestedProbeProjection : IProjectionFor<NestedProbe>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<NestedProbe> builder)
    {
        builder.From<ProbeProjectRegistered>(from =>
        {
            from.UsingKey(e => e.ProjectId);
            from.Set(m => m.Name).To(e => e.Name);
        });
        builder.Nested(m => m.Info, nested =>
        {
            nested.From<ProbeProjectRegistered>(from =>
            {
                from.UsingKey(e => e.ProjectId);
                from.Set(m => m.Name).To(e => e.Name);
            });
            nested.RemovedWith<ProbeProjectRenamed>(removed => removed.UsingKey(e => e.ProjectId));
        });
    }
}
