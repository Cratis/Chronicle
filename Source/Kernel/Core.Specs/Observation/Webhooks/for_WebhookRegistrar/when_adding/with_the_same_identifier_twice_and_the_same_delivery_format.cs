// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_the_same_identifier_twice_and_the_same_delivery_format : given.a_pinned_registration
{
    void Establish()
    {
        ALogWithLastLifecycleEvent(Cratis.Monads.Option<AppendedEvent>.None(), ALifecycleEvent<WebhookAdded>(0));
        TheStoredRegistrationHasFormat(WebhookDeliveryFormat.Legacy);
    }

    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition, _definition]));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_append_one_webhook_added() => ScopedAppends.Count().ShouldEqual(1);
}
