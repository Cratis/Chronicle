// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    DurableSubscriptionMode _durableSubscriptionMode = DurableSubscriptionMode.Off;
    DurableSubscriptionStance _durableSubscriptionStance = DurableSubscriptionStance.Preserve;
    ObserverSubscription? _durableSubscriptionSource;
    ObserverDurableSubscription? _durableSubscriptionRecorded;
    bool _forgottenDurableSubscriptionIsPending;

    /// <summary>
    /// What the next state write does with the durable subscription of the observer's state.
    /// </summary>
    enum DurableSubscriptionStance
    {
        /// <summary>Leave what the state holds, which is what storage held when the activation read it.</summary>
        Preserve = 0,

        /// <summary>Record the subscription the activation currently holds.</summary>
        Record = 1,

        /// <summary>Record none: the observer unsubscribed or this activation is leaving without a subscription.</summary>
        Clear = 2
    }

    /// <summary>
    /// Gets the durable subscription the next state write persists, as decision 0034 point 1 describes it.
    /// </summary>
    /// <returns>The durable subscription, or null for none.</returns>
    /// <remarks>
    /// Only a state write that follows a (re)subscription records it, so the record changes in the same write as the
    /// subscription. An activation that has not subscribed keeps what it read, so a progress write on an observer that
    /// was activated by something other than a subscription never erases a subscription it knows nothing about. The
    /// recorded value is cached against the live subscription, which is an immutable record replaced on every change.
    /// </remarks>
    ObserverDurableSubscription? DurableSubscriptionToWrite()
    {
        if (_durableSubscriptionMode == DurableSubscriptionMode.Off)
        {
            return null;
        }

        switch (_durableSubscriptionStance)
        {
            case DurableSubscriptionStance.Record:
                if (!ReferenceEquals(_durableSubscriptionSource, _subscription))
                {
                    _durableSubscriptionSource = _subscription;
                    _durableSubscriptionRecorded = _subscription.ToDurable();
                }

                return _durableSubscriptionRecorded;
            case DurableSubscriptionStance.Clear:
                return null;
            default:
                return State.DurableSubscription;
        }
    }

    void ConfigureDurableSubscriptions(DurableSubscriptionMode mode) => _durableSubscriptionMode = mode;

    /// <summary>
    /// Makes the next state writes record the subscription the activation holds.
    /// </summary>
    void RecordDurableSubscription()
    {
        _durableSubscriptionStance = DurableSubscriptionStance.Record;
        _forgottenDurableSubscriptionIsPending = false;
    }

    /// <summary>
    /// Stops recording a subscription and drops the one in the state, to be persisted by the next state write.
    /// </summary>
    void ForgetDurableSubscription()
    {
        _durableSubscriptionStance = DurableSubscriptionStance.Clear;
        _durableSubscriptionSource = null;
        _durableSubscriptionRecorded = null;
        if (State.DurableSubscription is null)
        {
            return;
        }

        State = State with { DurableSubscription = null };
        _forgottenDurableSubscriptionIsPending = true;
    }

    /// <summary>
    /// Persists the dropped durable subscription when the transition that followed it wrote nothing.
    /// </summary>
    /// <returns>A task that completes when the state is written, or at once when nothing is pending.</returns>
    async Task PersistForgottenDurableSubscription()
    {
        if (_forgottenDurableSubscriptionIsPending)
        {
            await WriteStateAsync();
        }
    }
}
