// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_Webhooks.when_listing;

public class with_unspecified_signing : given.webhook_query_responses
{
    void Establish() => _response.Signing = Contracts.Observation.Webhooks.WebhookSigningMode.Unspecified;

    async Task Because() => _definition = (await _webhooks.GetAll()).Single();

    [Fact] void should_show_unsigned() => _definition.Signing.ShouldEqual(WebhookSigningMode.None);
}
