// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>Projects a two-level nested object that can be cleared and re-established.</summary>
public class DeepNestedProbeProjection : IProjectionFor<DeepNestedProbe>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<DeepNestedProbe> builder)
    {
        builder.From<ProbeProjectRegistered>(from =>
        {
            from.UsingKey(e => e.ProjectId);
            from.Set(m => m.Name).To(e => e.Name);
        });
        builder.Nested(m => m.Outer, outer =>
        {
            outer.From<ProbeProjectRegistered>(from =>
            {
                from.UsingKey(e => e.ProjectId);
                from.Set(m => m.Name).To(e => e.Name);
            });
            outer.Nested(m => m.Info, info =>
            {
                info.From<ProbeProjectRegistered>(from =>
                {
                    from.UsingKey(e => e.ProjectId);
                    from.Set(m => m.Name).To(e => e.Name);
                });
                info.RemovedWith<ProbeProjectRenamed>(removed => removed.UsingKey(e => e.ProjectId));
            });
        });
    }
}
