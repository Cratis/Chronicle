// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverKeyHelper;

public class when_parsing_existing : Specification
{
    const string ObserverId = "1a07d613-278a-4b5d-ae87-eaf0890f00e4";
    const string EventStore = "7a28c4fa-cfd4-405e-9873-753bab4fd2e3";
    const string Namespace = "0c2adff8-7ac6-4998-bcec-c58c18d45f8f";
    const string EventSequenceId = "c7b1abce-9a6a-43aa-89e8-7ee6e154bdf7";
    string _combined = $"{ObserverId}{KeyHelper.Separator}{EventStore}{KeyHelper.Separator}{Namespace}{KeyHelper.Separator}{EventSequenceId}";

    ObserverKey _result;

    void Because() => _result = ObserverKey.Parse(_combined);

    [Fact] void should_hold_correct_observer_id() => _result.ObserverId.ToString().ShouldEqual(ObserverId);
    [Fact] void should_hold_correct_event_store() => _result.EventStore.ToString().ShouldEqual(EventStore);
    [Fact] void should_hold_correct_namespace() => _result.Namespace.ToString().ShouldEqual(Namespace);
    [Fact] void should_hold_correct_event_sequence_id() => _result.EventSequenceId.ToString().ShouldEqual(EventSequenceId);
}
