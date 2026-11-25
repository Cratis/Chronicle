// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Orleans.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_IAppendedEventsQueues;

public class when_inspecting_grain_call_interleaving : Specification
{
    bool _subscriptionProbeIsInterleaved;
    bool _enqueueAllowsInterleaving;
    bool _subscribeAllowsInterleaving;
    bool _subscribeToAllEventTypesAllowsInterleaving;
    bool _unsubscribeAllowsInterleaving;
    bool _spillToCatchupAllowsInterleaving;
    bool _spillMatchingToCatchupAllowsInterleaving;
    bool _tryMarkDormantAllowsInterleaving;
    bool _forgetAllowsInterleaving;
    bool _coordinatorIsReentrant;
    bool _coordinatorIsStatelessWorker;

    void Because()
    {
        _subscriptionProbeIsInterleaved = Array.TrueForAll(OverloadsOf(nameof(IAppendedEventsQueues.IsSubscribed)), method => Attribute.IsDefined(method, typeof(AlwaysInterleaveAttribute)));
        _enqueueAllowsInterleaving = AnyOverloadAllowsInterleaving(nameof(IAppendedEventsQueues.Enqueue));
        _subscribeAllowsInterleaving = AnyOverloadAllowsInterleaving(nameof(IAppendedEventsQueues.Subscribe));
        _subscribeToAllEventTypesAllowsInterleaving = AnyOverloadAllowsInterleaving(nameof(IAppendedEventsQueues.SubscribeToAllEventTypes));
        _unsubscribeAllowsInterleaving = AnyOverloadAllowsInterleaving(nameof(IAppendedEventsQueues.Unsubscribe));
        _spillToCatchupAllowsInterleaving = AnyOverloadAllowsInterleaving(nameof(IAppendedEventsQueues.SpillToCatchup));
        _spillMatchingToCatchupAllowsInterleaving = AnyOverloadAllowsInterleaving(nameof(IAppendedEventsQueues.SpillMatchingToCatchup));
        _tryMarkDormantAllowsInterleaving = AnyOverloadAllowsInterleaving(nameof(IAppendedEventsQueues.TryMarkDormant));
        _forgetAllowsInterleaving = AnyOverloadAllowsInterleaving(nameof(IAppendedEventsQueues.Forget));
        _coordinatorIsReentrant = Attribute.IsDefined(typeof(AppendedEventsQueues), typeof(ReentrantAttribute));
        _coordinatorIsStatelessWorker = Attribute.IsDefined(typeof(AppendedEventsQueues), typeof(StatelessWorkerAttribute));
    }

    [Fact] void should_interleave_the_subscription_probe() => _subscriptionProbeIsInterleaved.ShouldBeTrue();
    [Fact] void should_not_interleave_enqueueing_with_other_mutations() => _enqueueAllowsInterleaving.ShouldBeFalse();
    [Fact] void should_not_interleave_subscribing_with_other_mutations() => _subscribeAllowsInterleaving.ShouldBeFalse();
    [Fact] void should_not_interleave_subscribing_to_all_event_types_with_other_mutations() => _subscribeToAllEventTypesAllowsInterleaving.ShouldBeFalse();
    [Fact] void should_not_interleave_unsubscribing_with_other_mutations() => _unsubscribeAllowsInterleaving.ShouldBeFalse();
    [Fact] void should_not_interleave_spilling_to_catchup_with_other_mutations() => _spillToCatchupAllowsInterleaving.ShouldBeFalse();
    [Fact] void should_not_interleave_spilling_matching_events_to_catchup_with_other_mutations() => _spillMatchingToCatchupAllowsInterleaving.ShouldBeFalse();
    [Fact] void should_not_interleave_marking_dormant_with_other_mutations() => _tryMarkDormantAllowsInterleaving.ShouldBeFalse();
    [Fact] void should_not_interleave_forgetting_with_other_mutations() => _forgetAllowsInterleaving.ShouldBeFalse();
    [Fact] void should_not_make_the_coordinator_reentrant() => _coordinatorIsReentrant.ShouldBeFalse();
    [Fact] void should_not_make_the_coordinator_a_stateless_worker() => _coordinatorIsStatelessWorker.ShouldBeFalse();

    static MethodInfo[] OverloadsOf(string methodName)
    {
        var overloads = typeof(IAppendedEventsQueues).GetMethods().Where(method => method.Name == methodName).ToArray();
        overloads.ShouldNotBeEmpty();
        return overloads;
    }

    static bool AnyOverloadAllowsInterleaving(string methodName) => Array.Exists(
        OverloadsOf(methodName),
        method => Attribute.IsDefined(method, typeof(AlwaysInterleaveAttribute)) || Attribute.IsDefined(method, typeof(ReadOnlyAttribute)));
}
