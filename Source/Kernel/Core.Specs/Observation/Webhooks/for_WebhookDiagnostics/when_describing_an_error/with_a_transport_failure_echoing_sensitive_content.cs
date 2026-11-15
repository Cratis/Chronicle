// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.given;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDiagnostics.when_describing_an_error;

public class with_a_transport_failure_echoing_sensitive_content : Specification
{
    readonly string _host = SensitiveTarget.UniqueHost();
    string _diagnostic;

    void Because()
    {
        var error = new HttpRequestException(HttpRequestError.ConnectionError, SensitiveTarget.Reason, new AggregateException(new Exception(SensitiveTarget.QueryMarker)));
        error.Data["receiver"] = SensitiveTarget.Password;
        _diagnostic = WebhookDiagnostics.Describe(new(SensitiveTarget.WithCredentials(_host), WebhookAuthorization.None, new Dictionary<string, string>()), error);
    }

    [Fact] void should_not_retain_sensitive_content() => SensitiveTarget.Leaks(_diagnostic).ShouldBeFalse();
    [Fact] void should_keep_failure_classification() => _diagnostic.ShouldContain("ConnectionError");
    [Fact] void should_identify_safe_authority() => _diagnostic.ShouldContain($"https://{_host}:8443");
}
