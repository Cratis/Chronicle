// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class a_new_legacy_webhook_whose_identifier_has_observer_history : given.a_pinned_registration
{
    void Establish() => _storage.GetEventStore(Arg.Any<EventStoreName>()).Observers.Has(Arg.Any<ObserverId>()).Returns(true);

    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition]));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_append_one_webhook_added() => _appended.OfType<WebhookAdded>().Count().ShouldEqual(1);
}
