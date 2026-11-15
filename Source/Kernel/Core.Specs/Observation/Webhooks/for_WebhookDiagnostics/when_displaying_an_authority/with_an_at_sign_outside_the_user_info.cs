// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDiagnostics.when_displaying_an_authority;

public class with_an_at_sign_outside_the_user_info : Specification
{
    string _authority;

    void Because() => _authority = WebhookDiagnostics.Authority("https://hostileUser7d1c:4242424242424242/x@receiver.example/");

    [Fact] void should_use_opaque_text() => _authority.ShouldEqual(WebhookDiagnostics.Redacted);
}
