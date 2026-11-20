// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.Webhooks.for_Webhooks.when_getting_signing_keys;

public class requesting_the_event_store_name : given.signing_key_query_responses
{
    async Task Because() => await _webhooks.GetSigningKeys();

    [Fact] void should_request_for_the_event_store() => _request.EventStore.ShouldEqual("some-event-store");
}
