// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

public class and_a_per_value_claim_changes : a_revisable_sequence
{
    protected override UniqueConstraintMode UniqueMode => UniqueConstraintMode.PerValue;

    async Task Establish() => await _claims.Save(_eventSourceId, _uniqueDefinition, 0, ValueOf("claimed@example.com"));

    async Task Because() => await ReviseFirstEvent();

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] async Task should_release_the_claim_the_old_content_made() => (await _claims.IsAllowed("another-source", _uniqueDefinition, ValueOf("claimed@example.com"))).IsAllowed.ShouldBeTrue();
    [Fact] void should_have_persisted_the_lowered_watermark_before_storage_revised() => _watermarkWhenStorageRevised.ShouldEqual(EventSequenceNumber.First);
}
