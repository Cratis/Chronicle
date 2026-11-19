// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_Webhooks.when_getting_signing_keys;

public class with_a_failed_query : given.signing_key_query_responses
{
    Exception _exception;

    void Establish() => _result = new Contracts.Queries.QueryResult<IEnumerable<WebhookSigningPublicKeyResponse>> { ExceptionMessages = ["Key ring unavailable."] };

    async Task Because() => _exception = await Catch.Exception(_webhooks.GetSigningKeys);

    [Fact] void should_throw_query_failed() => _exception.ShouldBeOfExactType<WebhookSigningKeysQueryFailed>();
    [Fact] void should_preserve_the_message() => _exception.Message.ShouldContain("Key ring unavailable.");
}
