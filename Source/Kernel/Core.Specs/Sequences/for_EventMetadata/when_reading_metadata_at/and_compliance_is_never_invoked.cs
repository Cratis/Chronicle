// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_EventMetadata.when_reading_metadata_at;

public class and_compliance_is_never_invoked : given.a_metadata_query
{
    async Task Because() => await Read(42);

    [Fact] void should_only_read_metadata_from_event_storage() => _events.ReceivedCalls().Single().GetMethodInfo().Name.ShouldEqual("GetMetadataAt");
    [Fact] void should_only_read_requested_identities_without_cached_resolution() => _identities.ReceivedCalls().Single().GetMethodInfo().Name.ShouldEqual("GetByIds");
    [Fact] void should_have_no_payload_to_release() => typeof(EventMetadata).GetProperty("Content").ShouldBeNull();
    [Fact] void should_have_no_compliance_dependency() => typeof(EventMetadata).GetMethod("MetadataAt", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetParameters().Any(_ => _.ParameterType == typeof(Events.IEventCompliance)).ShouldBeFalse();
}
