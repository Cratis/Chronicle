// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Cratis.DependencyInjection;
using Cratis.Monads;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represents an implementation of <see cref="IWebhookMediator"/>.
/// </summary>
/// <param name="webhookHttpClientFactory">The <see cref="IWebhookHttpClientFactory"/>.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
/// <param name="options">Webhook delivery defaults.</param>
/// <param name="logger">Delivery logging.</param>
[Singleton]
public class WebhookMediator(IWebhookHttpClientFactory webhookHttpClientFactory, JsonSerializerOptions jsonSerializerOptions, IOptions<ChronicleOptions>? options = null, ILogger<WebhookMediator>? logger = null) : IWebhookMediator
{
    /// <inheritdoc/>
    public async Task<Catch> OnNext(EventStoreName? eventStore, WebhookTarget webhookTarget, Key partition, IEnumerable<AppendedEvent> events, string? accessToken = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var result = await Deliver(eventStore, webhookTarget, partition, events, accessToken, timeout, cancellationToken);
        return result.IsSuccess ? Catch.Success() : Catch.Failed(result.Error!);
    }

    /// <inheritdoc/>
    public async Task<WebhookDeliveryResult> Deliver(EventStoreName? eventStore, WebhookTarget webhookTarget, Key partition, IEnumerable<AppendedEvent> events, string? accessToken = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeout is { } duration && duration > TimeSpan.Zero)
        {
            deadline.CancelAfter(duration);
        }

        var batch = events.ToArray();
        try
        {
            var httpClient = webhookHttpClientFactory.Create(webhookTarget, accessToken);
            using var request = new HttpRequestMessage(HttpMethod.Post, string.Empty)
            {
                Content = JsonContent.Create(new EventsToObserve(partition.ToString(), batch), options: jsonSerializerOptions)
            };
            if (eventStore is not null) request.Options.Set(WebhookRequestOptions.EventStore, eventStore);
            using var response = await httpClient.SendAsync(request, deadline.Token);
            if (!response.IsSuccessStatusCode)
            {
                return new(EventSequenceNumber.Unavailable, new WebhookDeliveryFailed(webhookTarget.Url, response.StatusCode, response.ReasonPhrase));
            }

            return new(batch.LastOrDefault()?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable);
        }
        catch (BrokenCircuitException error)
        {
            var now = DateTimeOffset.UtcNow;
            var delay = error.RetryAfter ?? TimeSpan.FromSeconds(options?.Value.Webhooks.CircuitBreakerBreakDurationSeconds ?? 15);
            var authority = new Uri(webhookTarget.Url).GetLeftPart(UriPartial.Authority);
            WebhookDeliveryMetrics.CircuitRejected(authority);
            (logger ?? NullLogger<WebhookMediator>.Instance).TargetUnavailable(authority, now + delay);
            return new(EventSequenceNumber.Unavailable, error, new(Guid.NewGuid(), PartitionDeferralReason.CircuitOpen, now, now + delay, batch.FirstOrDefault()?.Context.SequenceNumber ?? EventSequenceNumber.First));
        }
        catch (Exception error)
        {
            return new(EventSequenceNumber.Unavailable, error);
        }
    }
}
