// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building;

public class with_not_rewindable : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Because() => _result = builder.Build(typeof(AuditLogEntry));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_not_be_rewindable() => _result.IsRewindable.ShouldBeFalse();
    [Fact] void should_be_active() => _result.IsActive.ShouldBeTrue();
    [Fact] void should_use_default_event_sequence() => _result.EventSequenceId.ShouldEqual(EventSequenceId.Log.Value);
}
