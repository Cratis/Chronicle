// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Kernel.given;

namespace Cratis.Chronicle.Projections.Kernel.for_KernelProjectionBuilder.when_building;

public class and_nothing_is_configured : Specification
{
    KernelProjectionBuilder<a_read_model> _builder;
    ProjectionDefinition _result;

    void Establish() => _builder = new(WellKnownKernelProjections.IdentifierFor("stats"), "stats");

    void Because() => _result = _builder.Build().Single();

    [Fact] void should_be_owned_by_the_kernel() => _result.Owner.ShouldEqual(ProjectionOwner.Kernel);
    [Fact] void should_be_recognized_as_kernel_owned() => _result.IsKernelOwned.ShouldBeTrue();
    [Fact] void should_not_be_rewindable() => _result.IsRewindable.ShouldBeFalse();
    [Fact] void should_be_active() => _result.IsActive.ShouldBeTrue();
    [Fact] void should_default_to_the_namespaced_scope() => _result.Scope.ShouldEqual(ProjectionScope.Namespaced);
    [Fact] void should_default_to_the_event_log() => _result.EventSequenceId.ShouldEqual(EventSequenceId.Log);
    [Fact] void should_default_to_identifying_by_event_source_id() => _result.Identifier.Value.ShouldEqual("$system.stats");
    [Fact] void should_not_subscribe_to_all_events() => _result.SubscribesToAllEvents.ShouldBeFalse();
}
