// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures;
using Cratis.Chronicle.Projections;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_namespace_subscriptions;

public class and_the_subscriptions_are_pending : given.an_event_sequence
{
    readonly TaskCompletionSource _pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IProjectionsManager _projections;
    ICapturesManager _captures;
    bool _tickCompletedWithoutSubscriptions;

    void Establish()
    {
        _projections = Substitute.For<IProjectionsManager>();
        _captures = Substitute.For<ICapturesManager>();
        _projections.EnsureSubscribedIn(EventStoreNamespace).Returns(_pending.Task);
        _captures.EnsureSubscribedIn(EventStoreNamespace).Returns(_pending.Task);
        _silo.AddProbe(_ => _projections);
        _silo.AddProbe(_ => _captures);
    }

    async Task Because()
    {
        var tick = _silo.TimerRegistry.FireAllAsync();
        _tickCompletedWithoutSubscriptions = tick.IsCompletedSuccessfully;
        _pending.TrySetResult();
        await tick.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_not_hold_the_event_sequence_turn_awaiting_subscriptions() => _tickCompletedWithoutSubscriptions.ShouldBeTrue();
    [Fact] async Task should_reconcile_projections_in_this_namespace() => await _projections.Received(1).EnsureSubscribedIn(EventStoreNamespace);
    [Fact] async Task should_reconcile_captures_in_this_namespace() => await _captures.Received(1).EnsureSubscribedIn(EventStoreNamespace);
}
