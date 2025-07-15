// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Grains.Observation.for_PartitionedObserverKey;

public class when_parsing_existing : Specification
{
    const string EventStore = "7a28c4fa-cfd4-405e-9873-753bab4fd2e3";
    const string Namespace = "0c2adff8-7ac6-4998-bcec-c58c18d45f8f";
    const string EventSequenceId = "c7b1abce-9a6a-43aa-89e8-7ee6e154bdf7";
    const string EventSourceId = "181d06d6-2b0a-49cc-802d-734e3fab2a9b";
    string _combined = $"{EventStore}{KeyHelper.Separator}{Namespace}{KeyHelper.Separator}{EventSequenceId}{KeyHelper.Separator}{EventSourceId}";

    PartitionedObserverKey _result;

    void Because() => _result = PartitionedObserverKey.Parse(_combined);

    [Fact] void should_hold_correct_microservice_id() => _result.EventStore.ToString().ShouldEqual(EventStore);
    [Fact] void should_hold_correct_tenant_id() => _result.Namespace.ToString().ShouldEqual(Namespace);
    [Fact] void should_hold_correct_event_sequence_id() => _result.EventSequenceId.ToString().ShouldEqual(EventSequenceId);
    [Fact] void should_hold_correct_event_source_id() => _result.EventSourceId.ToString().ShouldEqual(EventSourceId);
}
