// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactions;

namespace Cratis.Chronicle.Grains.Observation.for_PartitionedObserverKey;

public class when_parsing_existing : Specification
{
    const string @namespace = "0c2adff8-7ac6-4998-bcec-c58c18d45f8f";
    const string event_store = "7a28c4fa-cfd4-405e-9873-753bab4fd2e3";
    const string event_sequence_id = "c7b1abce-9a6a-43aa-89e8-7ee6e154bdf7";
    const string event_source_id = "181d06d6-2b0a-49cc-802d-734e3fab2a9b";
    const string combined = $"{event_store}+{@namespace}+{event_sequence_id}+{event_source_id}";

    PartitionedObserverKey result;

    void Because() => result = PartitionedObserverKey.Parse(combined);

    [Fact] void should_hold_correct_microservice_id() => result.EventStore.ToString().ShouldEqual(event_store);
    [Fact] void should_hold_correct_tenant_id() => result.Namespace.ToString().ShouldEqual(@namespace);
    [Fact] void should_hold_correct_event_sequence_id() => result.EventSequenceId.ToString().ShouldEqual(event_sequence_id);
    [Fact] void should_hold_correct_event_source_id() => result.EventSourceId.ToString().ShouldEqual(event_source_id);
}
