// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.given;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDiagnostics.when_describing_an_error;

public class with_a_timeout : Specification
{
    string _diagnostic;

    void Because() => _diagnostic = WebhookDiagnostics.Describe(new(SensitiveTarget.WithCredentials("receiver.example"), WebhookAuthorization.None, new Dictionary<string, string>()), new TaskCanceledException(SensitiveTarget.Reason));

    [Fact] void should_not_retain_sensitive_content() => SensitiveTarget.Leaks(_diagnostic).ShouldBeFalse();
    [Fact] void should_classify_timeout() => _diagnostic.ShouldContain("timed out");
}
