// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSigningKeys;

public class when_round_tripping_hmac_secrets : Specification
{
    bool[] _results;

    void Because() => _results = Enumerable.Range(24, 41).Select(length =>
    {
        var secret = Enumerable.Range(0, length).Select(_ => (byte)_).ToArray();
        var encoded = WebhookSigningKeys.EncodeHmacSecret(secret);
        return encoded == $"whsec_{Convert.ToBase64String(secret)}" &&
            WebhookSigningKeys.TryParseHmacSecret(encoded, out var parsed) && parsed.SequenceEqual(secret);
    }).ToArray();

    [Fact] void should_preserve_every_allowed_length() => _results.All(_ => _).ShouldBeTrue();
}
