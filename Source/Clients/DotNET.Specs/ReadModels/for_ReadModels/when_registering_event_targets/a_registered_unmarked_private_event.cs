// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering_event_targets;

public class a_registered_unmarked_private_event : given.a_registered_unmarked_target<a_registered_unmarked_private_event.Target>
{
    async Task Because() => await RegisterBoth();

    [Fact] void should_reject_bulk_outbox_publication() => _bulkError.ShouldBeOfExactType<PrivateEventTypeCannotBePublishedToOutbox>();
    [Fact] void should_reject_generic_outbox_publication() => _genericError.ShouldBeOfExactType<PrivateEventTypeCannotBePublishedToOutbox>();
    [Fact] void should_not_register_a_sink() => _definitions.ShouldBeEmpty();

    public record Target(string Name);
}
