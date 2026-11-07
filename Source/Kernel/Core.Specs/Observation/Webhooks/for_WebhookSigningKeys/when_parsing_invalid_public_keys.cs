// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSigningKeys;

public class when_parsing_invalid_public_keys : Specification
{
    string[] _values;
    bool[] _results;
    byte[][] _parsed;

    void Establish() => _values =
    [
        $"wrong_{Convert.ToBase64String(new byte[32])}",
        "whpk_%%%",
        $"whpk_{Convert.ToBase64String(new byte[31])}",
        $"whpk_{Convert.ToBase64String(new byte[33])}",
        $"whsk_{Convert.ToBase64String(new byte[32])}"
    ];

    void Because()
    {
        _parsed = new byte[_values.Length][];
        _results = _values.Select((value, index) => WebhookSigningKeys.TryParsePublicKey(value, out _parsed[index])).ToArray();
    }

    [Fact] void should_reject_every_invalid_value() => _results.Any(_ => _).ShouldBeFalse();
    [Fact] void should_return_no_key_material() => _parsed.All(_ => _.Length == 0).ShouldBeTrue();
}
