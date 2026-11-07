// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.given;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.given;

public class a_webhook_mediator_with_recorded_logs : a_webhook_mediator
{
    protected RecordingLogger<WebhookMediator> _logs;
    protected string _host;
    protected WebhookTarget _target;

    void Establish()
    {
        _host = SensitiveTarget.UniqueHost();
        _target = new(SensitiveTarget.WithCredentials(_host), WebhookAuthorization.None, new Dictionary<string, string>());
        _logs = new();
        _mediator = new(_httpClientFactory, _jsonOptions, logger: _logs);
    }
}
