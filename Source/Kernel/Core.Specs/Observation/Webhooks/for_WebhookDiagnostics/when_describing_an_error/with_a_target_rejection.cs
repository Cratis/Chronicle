// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDiagnostics.when_describing_an_error;

public class with_a_target_rejection : Specification
{
    string _diagnostic;

    void Because() => _diagnostic = WebhookDiagnostics.Describe(new("https://receiver.example/", WebhookAuthorization.None, new Dictionary<string, string>()), new WebhookTargetRejected("Webhook target must use HTTPS."));

    [Fact] void should_preserve_fixed_policy_text() => _diagnostic.ShouldEqual("Webhook target must use HTTPS.");
}
