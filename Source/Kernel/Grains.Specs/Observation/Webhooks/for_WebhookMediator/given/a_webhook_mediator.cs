// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Grains.Observation.Webhooks.for_WebhookMediator.given;

public class a_webhook_mediator : Specification
{
    protected WebhookMediator _mediator;
    protected IWebhookHttpClientFactory _httpClientFactory;
    protected JsonSerializerOptions _jsonOptions;

    void Establish()
    {
        _httpClientFactory = Substitute.For<IWebhookHttpClientFactory>();
        _jsonOptions = new JsonSerializerOptions();
        _mediator = new WebhookMediator(_httpClientFactory, _jsonOptions);
    }
}
