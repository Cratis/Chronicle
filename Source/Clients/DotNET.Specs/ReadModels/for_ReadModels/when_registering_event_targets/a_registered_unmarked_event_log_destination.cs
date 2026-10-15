// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering_event_targets;

public class a_registered_unmarked_event_log_destination : given.a_registered_unmarked_target<a_registered_unmarked_event_log_destination.Target>
{
    async Task Because() => await RegisterBoth();

    [Fact] void should_reject_bulk_event_log_publication() => _bulkError.ShouldBeOfExactType<EventLogIsNotAPublicationTarget>();
    [Fact] void should_reject_generic_event_log_publication() => _genericError.ShouldBeOfExactType<EventLogIsNotAPublicationTarget>();
    [Fact] void should_not_register_a_sink() => _definitions.ShouldBeEmpty();

    [PublishTo(EventSequenceId.LogId)]
    public record Target(string Name);
}
