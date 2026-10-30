// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class a_webhook_marked_inactive_while_a_client_serves_the_observer : given.a_webhook_registration_with_a_connected_client
{
    void Establish() => _definition.IsInactive = true;

    Task Because() => Register();

    [Fact] void should_register_the_inactive_definition() => ShouldAppend();
    [Fact] Task should_not_check_transport_ownership() => _clientObserver.DidNotReceive().GetSubscription();
}
