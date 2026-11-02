// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Properties;
using Cratis.Json;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope.given;

public class a_delivery_request : Specification
{
    protected HttpDeliveryRequest _request;
    protected DeliveryEnvelopeWriter _writer;
    protected JsonSerializerOptions _options;

    void Establish()
    {
        _options = new JsonSerializerOptions
        {
            Converters = { new EnumConverterFactory(), new EnumerableConceptAsJsonConverterFactory(), new ConceptAsJsonConverterFactory() }
        };
        _writer = new(_options);
        dynamic content = new ExpandoObject();
        content.Name = "Ada";
        var context = EventContext.From("storage-store", "storage-namespace", new EventType("customer-created", 2), EventSourceType.Default, "customer-1", EventStreamType.All, EventStreamId.Default, 42, CorrelationId.NotSet, [], DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        _request = new("store", "namespace", "observer", "reactor", "sequence", new(new("https://example.com/webhook"), WebhookAuthorization.None, new Dictionary<string, string>()), new Key("partition", ArrayIndexers.NoIndexers), [new(context, content)], DateTimeOffset.Parse("2026-01-02T00:00:00Z"));
    }
}
