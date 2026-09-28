// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionsExtensions.when_checking_has_model_bound_projection_attributes;

public class with_an_event_pinned_to_an_event_sequence : Specification
{
    bool _result;

    void Because() => _result = typeof(EventWithEventSequence).HasModelBoundProjectionAttributes();

    [Fact] void should_not_discover_the_event_as_a_projection() => _result.ShouldBeFalse();
}
