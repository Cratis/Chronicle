// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_generation_is_pinned;

[EventSequence("inbox-pinned-subscription-source")]
public class CurrentInboxReactor(SubscriptionDeliveryTracker tracker) : IReactor
{
    public Task On(SubscriptionPersonRegistered @event, EventContext context) => tracker.Delivered(@event.FullName, context);
}
