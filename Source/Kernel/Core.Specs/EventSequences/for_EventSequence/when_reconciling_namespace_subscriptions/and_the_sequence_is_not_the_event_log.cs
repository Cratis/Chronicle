// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Projections;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_namespace_subscriptions;

public class and_the_sequence_is_not_the_event_log : given.an_event_sequence
{
    IProjectionsManager _projections;
    ICapturesManager _captures;

    protected override EventSequenceId SequenceId => EventSequenceId.Outbox;

    void Establish()
    {
        _projections = Substitute.For<IProjectionsManager>();
        _captures = Substitute.For<ICapturesManager>();
        _silo.AddProbe(_ => _projections);
        _silo.AddProbe(_ => _captures);
    }

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] async Task should_not_start_another_projection_watchdog() => await _projections.DidNotReceive().EnsureSubscribedIn(EventStoreNamespace);
    [Fact] async Task should_not_start_another_capture_watchdog() => await _captures.DidNotReceive().EnsureSubscribedIn(EventStoreNamespace);
}
