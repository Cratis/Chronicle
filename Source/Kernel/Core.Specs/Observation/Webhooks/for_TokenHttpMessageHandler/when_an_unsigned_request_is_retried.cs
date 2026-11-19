// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_TokenHttpMessageHandler;

public class when_an_unsigned_request_is_retried : given.a_signed_token_delivery
{
    void Establish() => _identity = null;
    Task Because() => Deliver();

    [Fact] void should_deliver() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_the_body() => _receiver.Bodies[1].ShouldEqual(_receiver.Bodies[0]);
    [Fact] void should_not_add_signing_headers() => _receiver.Headers.TrueForAll(headers => !headers.Keys.Any(key => key.StartsWith("webhook-", StringComparison.OrdinalIgnoreCase) || key.Equals("Cratis-Key-Id", StringComparison.OrdinalIgnoreCase))).ShouldBeTrue();
    [Fact] void should_preserve_other_headers() => _receiver.Headers[1].Where(header => header.Key != "Authorization").Select(header => header.Key).ShouldEqual(_receiver.Headers[0].Where(header => header.Key != "Authorization").Select(header => header.Key));
}
