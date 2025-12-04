// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionsExtensions.when_checking_has_model_bound_projection_attributes;

public class with_not_rewindable_attribute : Specification
{
    bool _result;

    void Because() => _result = typeof(TypeWithNotRewindableAttribute).HasModelBoundProjectionAttributes();

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
