// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

public class and_a_per_value_claim_is_stamped_at_another_position : a_revisable_sequence
{
    protected override UniqueConstraintMode UniqueMode => UniqueConstraintMode.PerValue;

    async Task Establish() => await _claims.Save(_eventSourceId, _uniqueDefinition, 5, ValueOf("claimed@example.com"));

    async Task Because() => await ReviseFirstEvent();

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] async Task should_keep_the_claim_another_event_made() => (await _claims.IsAllowed("another-source", _uniqueDefinition, ValueOf("claimed@example.com"))).IsAllowed.ShouldBeFalse();
}
