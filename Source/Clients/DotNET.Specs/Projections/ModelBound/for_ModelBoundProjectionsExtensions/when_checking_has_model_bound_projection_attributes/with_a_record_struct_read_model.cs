// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionsExtensions.when_checking_has_model_bound_projection_attributes;

public class with_a_record_struct_read_model : Specification
{
    bool _result;

    void Because() => _result = typeof(StructWithFromEventAttribute).HasModelBoundProjectionAttributes();

    [Fact] void should_remain_discoverable_for_registration() => _result.ShouldBeTrue();
}
