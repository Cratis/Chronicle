// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookSignatureVerifier.when_constructing;

public class with_a_malformed_key : Specification
{
    Exception[] _errors;

    static WebhookSignatureVerifier Create(string key) => new([key]);

    void Because() => _errors =
    [
        Catch.Exception(() => Create("not-a-key")),
        Catch.Exception(() => Create("whpk_AAAA")),
        Catch.Exception(() => Create("whpk_!!!"))
    ];

    [Fact] void should_reject_every_key() => _errors.All(_ => _ is ArgumentException).ShouldBeTrue();
}
