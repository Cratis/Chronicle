// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionsExtensions.when_checking_has_model_bound_projection_attributes;

public class with_only_a_not_projected_member : Specification
{
    bool _result;

    void Because() => _result = typeof(ModelWithOnlyNotProjectedMember).HasModelBoundProjectionAttributes();

    [Fact] void should_not_be_a_model_bound_projection() => _result.ShouldBeFalse();

    record ModelWithOnlyNotProjectedMember(string Name, [property: NotProjected] string Assembled);
}
