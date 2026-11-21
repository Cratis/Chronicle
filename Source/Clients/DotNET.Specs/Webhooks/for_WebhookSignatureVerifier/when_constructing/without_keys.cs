// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookSignatureVerifier.when_constructing;

public class without_keys : Specification
{
    Exception _error;

    static WebhookSignatureVerifier Create() => new(Array.Empty<string>());

    void Because() => _error = Catch.Exception(() => Create());

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<ArgumentException>();
}
