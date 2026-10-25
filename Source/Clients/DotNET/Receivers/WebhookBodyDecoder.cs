// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Serverless;
using Cratis.Json;
using ContractEvent = Cratis.Chronicle.Contracts.Events.AppendedEvent;

namespace Cratis.Chronicle.Receivers;

/// <summary>
/// Decodes today's kernel webhook body using the kernel's JSON naming and converters.
/// </summary>
/// <param name="limits">The local delivery limits.</param>
internal sealed class WebhookBodyDecoder(ServerlessDeliveryLimits limits) : IDeliveryDecoder
{
    /// <summary>
    /// Kernel-shaped options, independent of the application's configurable naming policy.
    /// </summary>
    static readonly JsonSerializerOptions _options = CreateOptions();

    /// <inheritdoc/>
    public async Task<Delivery> Decode(DeliveryRequest request, CancellationToken cancellationToken)
    {
        if (request.Target is null)
        {
            throw new MalformedDelivery("A delivery target is required.");
        }

        await using var body = await ReadBody(request.Body, cancellationToken);
        try
        {
            var webhook = await JsonSerializer.DeserializeAsync<WebhookBody>(body, _options, cancellationToken);
            if (webhook is null || string.IsNullOrWhiteSpace(webhook.Partition) || webhook.Events is null)
            {
                throw new MalformedDelivery("Invalid webhook body.");
            }

            var events = webhook.Events.Select(ToContract).ToArray();
            return new(request.Target, webhook.Partition, events, null);
        }
        catch (JsonException)
        {
            throw new MalformedDelivery("Invalid webhook JSON.");
        }
    }

    /// <summary>
    /// Converts only the released public context and event content.
    /// </summary>
    /// <param name="item">The wire event.</param>
    /// <returns>The event for the existing dispatcher.</returns>
    /// <exception cref="MalformedDelivery">The event is missing required context or content.</exception>
    static ContractEvent ToContract(WebhookBodyEvent item)
    {
        if (item is null || item.Context is null || item.Content is null ||
            item.Context.EventType is null || item.Context.Causation is null || item.Context.CausedBy is null || item.Context.Tags is null)
        {
            throw new MalformedDelivery("Invalid webhook event.");
        }

        return new()
        {
            Context = item.Context.ToContract(),
            Content = item.Content.ToJsonString(),
            GenerationalContent = item.GenerationalContent is null ? [] : new Dictionary<int, string>(item.GenerationalContent)
        };
    }

    /// <summary>
    /// Creates options matching the kernel webhook serializer rather than host JSON options.
    /// </summary>
    /// <returns>The decoder-owned JSON options.</returns>
    static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new EventObservationStateJsonConverter());
        options.Converters.Add(new ComplexKeyDictionaryJsonConverterFactory());
        options.Converters.Add(new EnumConverterFactory());
        options.Converters.Add(new EventSourceIdJsonConverterFactory());
        options.Converters.Add(new EnumerableConceptAsJsonConverterFactory());
        options.Converters.Add(new ConceptAsJsonConverterFactory());
        return options;
    }

    /// <summary>
    /// Reads no more than the body limit plus one byte without disposing the transport stream.
    /// </summary>
    /// <param name="source">The transport-owned body stream.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The buffered body, owned by the caller.</returns>
    /// <exception cref="DeliveryTooLarge">The body exceeds the configured size limit.</exception>
    async Task<MemoryStream> ReadBody(Stream source, CancellationToken cancellationToken)
    {
        var body = new MemoryStream();
        try
        {
            var buffer = new byte[81920];
            while (true)
            {
                var remaining = limits.MaximumBodySize - body.Length;
                var size = remaining >= buffer.Length ? buffer.Length : (int)Math.Max(1, remaining + 1);
                var count = await source.ReadAsync(buffer.AsMemory(0, size), cancellationToken);
                if (count == 0)
                {
                    body.Position = 0;
                    return body;
                }

                if (count > remaining)
                {
                    throw new DeliveryTooLarge(limits.MaximumBodySize);
                }

                await body.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
            }
        }
        catch
        {
            await body.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Represents the legacy webhook body, which carries no target or reducer state.
    /// </summary>
    /// <param name="Partition">The observed partition.</param>
    /// <param name="Events">The delivered events.</param>
    sealed record WebhookBody(string? Partition, IReadOnlyList<WebhookBodyEvent>? Events);

    /// <summary>
    /// Represents one event in a kernel webhook body.
    /// </summary>
    /// <param name="Context">The public event context.</param>
    /// <param name="Content">The released event content.</param>
    sealed record WebhookBodyEvent(EventContext? Context, JsonObject? Content)
    {
        /// <summary>
        /// Gets the stored generational content, retained for dispatcher parity.
        /// </summary>
        public IReadOnlyDictionary<int, string>? GenerationalContent { get; init; }
    }
}
