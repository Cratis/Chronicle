// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using System.Dynamic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Observation;
using Cratis.Json;
using KernelBody = KernelConcepts::Cratis.Chronicle.Concepts.Observation.Webhooks.EventsToObserve;
using KernelEvents = KernelConcepts::Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Receivers.for_WebhookBodyDecoder.given;

public class a_kernel_webhook_body : Specification
{
    static readonly JsonSerializerOptions _options = new()
    {
        // Mirrors Source/Kernel/Core/Setup/Serialization/SerializationConfigurationExtensions.cs L150.
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new ComplexKeyDictionaryJsonConverterFactory(),
            new EnumConverterFactory(),
            new EnumerableConceptAsJsonConverterFactory(),
            new ConceptAsJsonConverterFactory()
        }
    };

    protected MemoryStream _body;
    protected Exception _error;
    private protected Delivery _delivery;
    private protected virtual DeliveryTarget? Target => new(ObserverType.Reactor, "handler", "store", "tenant");
    protected virtual string Body => Serialize();
    protected virtual long MaximumBodySize => 16 * 1024 * 1024;

    void Establish() => _body = new(Encoding.UTF8.GetBytes(Body));

    protected async Task Decode()
    {
        var decoder = new WebhookBodyDecoder(new() { MaximumBodySize = MaximumBodySize });
        _error = await Catch.Exception(async () => _delivery = await decoder.Decode(new(_body, Target), CancellationToken.None));
    }

    internal static string Serialize(KernelEvents.EventObservationState state = KernelEvents.EventObservationState.Initial)
    {
        var context = KernelEvents.EventContext.Empty with
        {
            EventType = new("orders", 2),
            SequenceNumber = 12,
            AppendedGeneration = 1,
            EventStore = "original-store",
            Namespace = "original-namespace",
            EventSourceId = "order-42",
            EventSource = "orders",
            Subject = "subject",
            NamedTags = [new("region", "west")],
            ObservationState = state
        };
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["order"] = 42;
        var body = new KernelBody("order-42", [new(context, content)
        {
            GenerationalContent = new Dictionary<int, string> { [1] = "{\"order\":42}", [2] = "{\"orderId\":42}" }
        }]);
        return JsonSerializer.Serialize(body, _options);
    }

    void Destroy() => _body?.Dispose();
}
