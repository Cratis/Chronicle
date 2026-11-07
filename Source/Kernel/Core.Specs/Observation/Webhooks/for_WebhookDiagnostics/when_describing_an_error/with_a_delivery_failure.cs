// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.given;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDiagnostics.when_describing_an_error;

public class with_a_delivery_failure : Specification
{
    string _diagnostic;

    void Because()
    {
        var target = new WebhookTarget(SensitiveTarget.WithCredentials("receiver.example"), WebhookAuthorization.None, new Dictionary<string, string>());
        _diagnostic = WebhookDiagnostics.Describe(target, new WebhookDeliveryFailed(target.Url, HttpStatusCode.InternalServerError, SensitiveTarget.Reason));
    }

    [Fact] void should_not_retain_sensitive_content() => SensitiveTarget.Leaks(_diagnostic).ShouldBeFalse();
    [Fact] void should_keep_status() => _diagnostic.ShouldContain("HTTP 500");
}
