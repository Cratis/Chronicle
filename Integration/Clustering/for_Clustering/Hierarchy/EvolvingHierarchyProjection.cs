// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering.Hierarchy;

public class EvolvingHierarchyProjection : IProjectionFor<EvolvingModule>
{
    public static bool Evolved { get; set; }

    public void Define(IProjectionBuilderFor<EvolvingModule> builder)
    {
        builder
            .From<HierarchyModuleAdded>(module =>
            {
                module.Set(m => m.Name).To(e => e.Name);

                // The new build's definition differs from the old one, which is what makes Chronicle replay it.
                if (Evolved)
                {
                    module.Set(m => m.Label).To(e => e.Name);
                }
            })
            .Children(m => m.Features, features => features
                .IdentifiedBy(f => f.Id)
                .From<HierarchyFeatureAdded>(feature => feature
                    .UsingParentKey(e => e.ModuleId)
                    .UsingKey(e => e.FeatureId)
                    .Set(f => f.Name).To(e => e.Name))
                .Children(f => f.Slices, slices => slices
                    .IdentifiedBy(s => s.Id)
                    .From<HierarchySliceAdded>(slice => slice
                        .UsingParentKey(e => e.FeatureId)
                        .UsingKey(e => e.SliceId)
                        .Set(s => s.Name).To(e => e.Name))
                    .Children(s => s.Events, events => events
                        .IdentifiedBy(i => i.Id)
                        .From<HierarchyEventAdded>(item => item
                            .UsingParentKey(e => e.SliceId)
                            .UsingKey(e => e.EventItemId)
                            .Set(i => i.Name).To(e => e.Name)))));
    }
}
