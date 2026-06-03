// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_nested_object;

public class SliceProjection : IProjectionFor<NestedSlice>
{
    public void Define(IProjectionBuilderFor<NestedSlice> builder) => builder
        .From<SliceCreated>(b => b.Set(m => m.Name).To(e => e.Name))
        .Nested(_ => _.Command, nested => nested
            .From<CommandSetForSlice>()
            .From<SliceCommandRenamed>(b => b.Set(m => m.Name).To(e => e.NewName))
            .ClearWith<CommandClearedForSlice>());
}
