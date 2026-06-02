// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_nested_in_children;

public class FeatureProjection : IProjectionFor<FeatureReadModel>
{
    public void Define(IProjectionBuilderFor<FeatureReadModel> builder) => builder
        .From<FeatureCreated>(b => b.Set(m => m.Name).To(e => e.Name))
        .Children(_ => _.Slices, slices => slices
            .IdentifiedBy(_ => _.Id)
            .From<SliceAddedToFeature>(b => b
                .UsingParentKey(e => e.FeatureId)
                .UsingKey(e => e.SliceId))
            .Nested(_ => _.Command, nested => nested
                .From<CommandSetOnSlice>()
                .From<CommandRenamedOnSlice>(b => b.Set(m => m.Name).To(e => e.NewName))
                .ClearWith<CommandClearedFromSlice>()));
}
