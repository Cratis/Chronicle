// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventStoreSubscriptions;

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_IEventStoreSubscriptionBuilder.given;

public class a_legacy_builder : Specification
{
    protected IEventStoreSubscriptionBuilder _builder;
    void Establish() => _builder = new LegacyBuilder();

    class LegacyBuilder : IEventStoreSubscriptionBuilder
    {
        public IEventStoreSubscriptionBuilder WithEventType<TEvent>() => this;
        public IEventStoreSubscriptionBuilder WithEventType(EventTypeId eventTypeId) => this;
        public EventStoreSubscriptionDefinition Build() => new("subscription", "source", []);
    }
}
