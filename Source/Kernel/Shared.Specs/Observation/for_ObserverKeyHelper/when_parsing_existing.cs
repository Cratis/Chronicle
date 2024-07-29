// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Grains.Observation.for_ObserverKeyHelper;

public class when_parsing_existing : Specification
{
    const string observer_id = "1a07d613-278a-4b5d-ae87-eaf0890f00e4";
    const string event_store = "7a28c4fa-cfd4-405e-9873-753bab4fd2e3";
    const string @namespace = "0c2adff8-7ac6-4998-bcec-c58c18d45f8f";
    const string event_sequence_id = "c7b1abce-9a6a-43aa-89e8-7ee6e154bdf7";
    const string combined = $"{observer_id}+{event_store}+{@namespace}+{event_sequence_id}";

    ObserverKey result;

    void Because() => result = ObserverKey.Parse(combined);

    [Fact] void should_hold_correct_observer_id() => result.ObserverId.ToString().ShouldEqual(observer_id);
    [Fact] void should_hold_correct_event_store() => result.EventStore.ToString().ShouldEqual(event_store);
    [Fact] void should_hold_correct_namespace() => result.Namespace.ToString().ShouldEqual(@namespace);
    [Fact] void should_hold_correct_event_sequence_id() => result.EventSequenceId.ToString().ShouldEqual(event_sequence_id);
}
