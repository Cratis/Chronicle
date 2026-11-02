// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class with_a_new_webhook_and_no_history : given.a_lagging_legacy_registration
{
    void Establish() => ALogWithLastLifecycleEvent(Cratis.Monads.Option<Concepts.Events.AppendedEvent>.None());

    /// <summary>Source equivalence: base 130c50244a WebhookRegistrar.cs:85-109 and :421-428 append New with the legacy default.</summary>
    /// <returns>Awaitable task.</returns>
    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition]));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_keep_the_legacy_default() => _appended.OfType<WebhookAdded>().Single().DeliveryFormat.ShouldEqual(WebhookDeliveryFormat.Legacy);
    [Fact] void should_append_the_authorization() => _appended.OfType<BearerTokenAuthorizationSetForWebhook>().Count().ShouldEqual(1);
    [Fact] void should_not_fence_the_append() => ScopedAppends.ShouldBeEmpty();
}
