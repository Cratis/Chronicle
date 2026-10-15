// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_IEventStoreSubscriptionBuilder.when_selecting_delivery;

public class with_compatibility : given.a_legacy_builder
{
    IEventStoreSubscriptionBuilder _result;
    void Because() => _result = _builder.WithGenerationDelivery(EventGenerationDelivery.Compatibility);
    [Fact] void should_keep_old_implementations_compatible() => _result.ShouldBeSame(_builder);
}
