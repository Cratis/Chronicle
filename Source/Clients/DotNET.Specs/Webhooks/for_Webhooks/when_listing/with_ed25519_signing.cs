// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_Webhooks.when_listing;

public class with_ed25519_signing : given.webhook_query_responses
{
    void Establish() => _response.Signing = Contracts.Observation.Webhooks.WebhookSigningMode.Ed25519;

    async Task Because() => _definition = (await _webhooks.GetAll()).Single();

    [Fact] void should_show_ed25519_signing() => _definition.Signing.ShouldEqual(WebhookSigningMode.Ed25519);
}
