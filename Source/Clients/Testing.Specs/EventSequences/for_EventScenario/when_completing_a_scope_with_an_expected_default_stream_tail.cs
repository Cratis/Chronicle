// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_completing_a_scope_with_an_expected_default_stream_tail : Specification, IDisposable
{
    EventScenario _scenario;
    bool _completed;

    async Task Establish()
    {
        _scenario = new();
        (await _scenario.EventLog.Append("source-a", new TestEvent("default"))).ShouldBeSuccessful();
        (await _scenario.EventLog.Append("source-a", new TestEvent("other"), "transactions", "month")).ShouldBeSuccessful();
    }

    async Task Because() => _completed = (await _scenario.EventSequence.CompleteStream(
        new ClosedStreamScope(EventSourceId: "source-a", EventStreamType: EventStreamType.All, EventStreamId: EventStreamId.Default), EventSequenceNumber.First)).IsSuccess;

    [Fact] void should_ignore_a_later_event_in_another_stream() => _completed.ShouldBeTrue();

    public void Dispose() => _scenario.Dispose();
}
