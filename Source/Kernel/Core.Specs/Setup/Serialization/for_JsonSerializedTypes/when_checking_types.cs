// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Setup.Serialization.for_JsonSerializedTypes;

public class when_checking_types : Specification
{
    [Fact] void should_exclude_cratis_exceptions() => JsonSerializedTypes.Includes(typeof(ClientIsNotConnected)).ShouldBeFalse();
    [Fact] void should_exclude_system_exceptions() => JsonSerializedTypes.Includes(typeof(InvalidOperationException)).ShouldBeFalse();
    [Fact] void should_exclude_kernel_payloads_that_have_generated_serializers() => JsonSerializedTypes.Includes(typeof(ConnectionId)).ShouldBeFalse();
    [Fact] void should_exclude_kernel_records() => JsonSerializedTypes.Includes(typeof(EventType)).ShouldBeFalse();
    [Fact] void should_include_choreography_types() => JsonSerializedTypes.Includes(typeof(JobState)).ShouldBeTrue();
    [Fact] void should_include_job_step_states() => JsonSerializedTypes.Includes(typeof(HandleEventsForObserverState)).ShouldBeTrue();
    [Fact] void should_include_event_objects() => JsonSerializedTypes.Includes(typeof(EventStoreAdded)).ShouldBeTrue();
    [Fact] void should_include_oneof_marker_types() => JsonSerializedTypes.Includes(typeof(OneOf.Types.None)).ShouldBeTrue();
    [Fact] void should_exclude_unrelated_types() => JsonSerializedTypes.Includes(typeof(Uri)).ShouldBeFalse();
}
