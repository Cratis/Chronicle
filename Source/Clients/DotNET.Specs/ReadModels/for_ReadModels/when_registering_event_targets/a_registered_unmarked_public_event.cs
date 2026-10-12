// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering_event_targets;

public class a_registered_unmarked_public_event : given.a_registered_unmarked_target<a_registered_unmarked_public_event.Target>
{
    async Task Because() => await RegisterBoth();

    [Fact] void should_register_both_paths() => _definitions.Count.ShouldEqual(2);
    [Fact] void should_use_event_sequence_sinks() => _definitions.TrueForAll(definition => definition.Sink.TypeId == SinkTypeId.EventSequence).ShouldBeTrue();
    [Fact] void should_preserve_registry_ids() => _definitions.TrueForAll(definition => definition.Sink.EventSequence.EventType.Id == "registered-target").ShouldBeTrue();
    [Fact] void should_preserve_registry_generations() => _definitions.TrueForAll(definition => definition.Sink.EventSequence.EventType.Generation == 4).ShouldBeTrue();
    [Fact] void should_publish_to_the_public_outbox() => _definitions.TrueForAll(definition => definition.Sink.EventSequence.IsPublic && definition.Sink.EventSequence.EventSequence == "outbox").ShouldBeTrue();
    [Fact] void should_not_take_the_model_bound_registration_shortcut() => _projections.DidNotReceive().Register<Target>();
    [Fact] void should_not_fail_bulk_registration() => _bulkError.ShouldBeNull();
    [Fact] void should_not_fail_generic_registration() => _genericError.ShouldBeNull();

    [Public]
    [FromEvent<Source>]
    public record Target(string Name);
    [EventType]
    public record Source(string Name);
}
