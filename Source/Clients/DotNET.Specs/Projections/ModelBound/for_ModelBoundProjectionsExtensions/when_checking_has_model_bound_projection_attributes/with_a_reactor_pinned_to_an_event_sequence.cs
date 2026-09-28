// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionsExtensions.when_checking_has_model_bound_projection_attributes;

public class with_a_reactor_pinned_to_an_event_sequence : Specification
{
    bool _eventLogResult;
    bool _eventSequenceResult;

    void Because()
    {
        _eventLogResult = typeof(ReactorWithEventLog).HasModelBoundProjectionAttributes();
        _eventSequenceResult = typeof(ReactorWithEventSequence).HasModelBoundProjectionAttributes();
    }

    [Fact] void should_not_discover_a_reactor_with_event_log_as_a_projection() => _eventLogResult.ShouldBeFalse();
    [Fact] void should_not_discover_a_reactor_with_event_sequence_as_a_projection() => _eventSequenceResult.ShouldBeFalse();
}
