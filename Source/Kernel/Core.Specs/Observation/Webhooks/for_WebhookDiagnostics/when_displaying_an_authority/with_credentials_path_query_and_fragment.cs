// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.given;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDiagnostics.when_displaying_an_authority;

public class with_credentials_path_query_and_fragment : Specification
{
    string _authority;

    void Because() => _authority = WebhookDiagnostics.Authority(SensitiveTarget.WithCredentials("receiver.example"));

    [Fact] void should_project_scheme_host_and_port_only() => _authority.ShouldEqual("https://receiver.example:8443");
    [Fact] void should_omit_sensitive_content() => SensitiveTarget.Leaks(_authority).ShouldBeFalse();
}
