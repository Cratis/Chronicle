// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope.given;

public class a_batch_delivery : a_delivery_request
{
    protected recording_http_handler _handler;
    protected WebhookMediator _mediator;

    void Establish()
    {
        var original = _request.Events[0];
        _request = _request with
        {
            Events = new ulong[] { 40, 42, 45 }.Select(number => original with { Context = original.Context with { SequenceNumber = number } }).ToArray(),
            DeliverBy = DateTimeOffset.UtcNow.AddMinutes(1)
        };
        _handler = new();
        var factory = Substitute.For<IWebhookHttpClientFactory>();
        factory.Create(Arg.Any<Concepts.Observation.Webhooks.WebhookTarget>(), Arg.Any<string?>()).Returns(new HttpClient(_handler) { BaseAddress = new("https://example.com/webhook") });
        _mediator = new(factory, _options);
    }

    protected string Result(object state, ulong? checkpoint, string? partition = null, string extra = "") =>
        $"{{\"results\":[{{\"partition\":{JsonSerializer.Serialize(partition ?? _request.Partition.ToString())},\"state\":{JsonSerializer.Serialize(state)},\"lastSuccessfulObservation\":{JsonSerializer.Serialize(checkpoint)}{extra}}}]}}";
}
