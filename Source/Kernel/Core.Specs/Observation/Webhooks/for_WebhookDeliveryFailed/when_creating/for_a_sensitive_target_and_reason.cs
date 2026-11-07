// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.given;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDeliveryFailed.when_creating;

public class for_a_sensitive_target_and_reason : Specification
{
    readonly string _host = SensitiveTarget.UniqueHost();
    WebhookDeliveryFailed _error;

    void Because() => _error = new(SensitiveTarget.WithCredentials(_host), HttpStatusCode.InternalServerError, SensitiveTarget.Reason);

    [Fact] void should_not_expose_sensitive_message() => SensitiveTarget.Leaks(_error.Message).ShouldBeFalse();
    [Fact] void should_not_expose_sensitive_exception_text() => SensitiveTarget.Leaks(_error.ToString()).ShouldBeFalse();
    [Fact] void should_project_target_property() => _error.TargetUrl.ShouldEqual((WebhookTargetUrl)$"https://{_host}:8443");
    [Fact] void should_omit_reason_property() => _error.ReasonPhrase.ShouldBeNull();
    [Fact] void should_keep_status_in_message() => _error.Message.ShouldContain("HTTP 500");
    [Fact] void should_identify_safe_authority() => _error.Message.ShouldContain($"https://{_host}:8443");
}
