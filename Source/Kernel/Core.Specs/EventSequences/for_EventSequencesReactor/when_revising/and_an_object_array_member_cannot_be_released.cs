// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesReactor.when_revising;

public class and_an_object_array_member_cannot_be_released : and_release_fails
{
    protected override bool UsesObjectArray => true;

    [Fact] void should_fail_instead_of_revising_with_blank_members() => _error.ShouldBeOfExactType<SchemaMetadataActionFailed>();
    [Fact] void should_not_call_the_revision_grain_with_blank_members() => _sequence.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "Revise").ShouldBeFalse();
    [Fact] void should_not_write_a_revision_with_blank_members() => _sequenceStorage.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "Revise").ShouldBeFalse();
    [Fact] void should_leave_existing_array_content_unchanged() => _original.ToJsonString().ShouldEqual(_originalText);
}
