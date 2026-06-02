// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_nested_in_nested;

public class SliceProjection : IProjectionFor<DeepNestedSlice>
{
    public void Define(IProjectionBuilderFor<DeepNestedSlice> builder) => builder
        .From<DeepNestedSliceCreated>(b => b.Set(m => m.Name).To(e => e.Name))
        .Nested(_ => _.Command, command => command
            .From<DeepNestedCommandSet>(b => b.Set(m => m.Name).To(e => e.Name))
            .Nested(_ => _.Validation, validation => validation
                .From<DeepNestedValidationConfigured>(b => b.Set(m => m.Rules).To(e => e.Rules))
                .From<DeepNestedValidationUpdated>(b => b.Set(m => m.Rules).To(e => e.NewRules))
                .ClearWith<DeepNestedValidationRemoved>())
            .ClearWith<DeepNestedCommandCleared>());
}
