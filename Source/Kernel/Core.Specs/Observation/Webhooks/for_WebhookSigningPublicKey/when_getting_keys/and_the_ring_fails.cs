// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSigningPublicKey.when_getting_keys;

public class and_the_ring_fails : given.a_public_key_query
{
    void Establish() => _ring.GetSigningKeys().Returns(_ => Task.FromException<IEnumerable<WebhookSigningKey>>(new InvalidOperationException("private-material-must-not-escape")));
    async Task Because() => _error = await Catch.Exception(Query);

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<WebhookSigningUnavailable>();
    [Fact] void should_not_retain_the_cause() => _error!.InnerException.ShouldBeNull();
    [Fact] void should_not_expose_the_failure_message() => _error!.ToString().Contains("private-material-must-not-escape", StringComparison.Ordinal).ShouldBeFalse();
}
