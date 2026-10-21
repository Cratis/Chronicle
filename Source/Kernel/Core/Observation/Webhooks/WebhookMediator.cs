// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net;
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
                return DeliveryRejected(response, webhookTarget, batch);
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

    /// <summary>
    /// Gets a valid receiver-provided retry deadline without applying delivery-specific limits.
    /// </summary>
    /// <param name="response">The receiver response.</param>
    /// <param name="maximumSeconds">The maximum permitted delay in seconds.</param>
    /// <returns>The retry deadline, or null for an invalid or expired header.</returns>
    internal static DateTimeOffset? GetRetryAfter(HttpResponseMessage response, int maximumSeconds = 24 * 60 * 60)
    {
        if (!response.Headers.TryGetValues("Retry-After", out var values)) return null;
        var value = values.FirstOrDefault();
        var now = DateTimeOffset.UtcNow;
        var maximum = TimeSpan.FromSeconds(Math.Max(1, maximumSeconds));
        if (long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
        {
            return now + TimeSpan.FromSeconds(Math.Clamp(seconds, 1, (long)maximum.TotalSeconds));
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date) && date > now)
        {
            return date - now > maximum ? now + maximum : date;
        }

        return null;
    }

    WebhookDeliveryResult DeliveryRejected(HttpResponseMessage response, WebhookTarget target, AppendedEvent[] batch)
    {
        var error = new WebhookDeliveryFailed(target.Url, response.StatusCode, response.ReasonPhrase);
        var authority = new Uri(target.Url).GetLeftPart(UriPartial.Authority);
        var deliveryLogger = logger ?? NullLogger<WebhookMediator>.Instance;
        if ((response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable) && GetRetryAfter(response, options?.Value.Webhooks.MaximumRetryAfterSeconds ?? 24 * 60 * 60) is { } until)
        {
            var now = DateTimeOffset.UtcNow;
            deliveryLogger.TargetRateLimited(authority, (int)response.StatusCode, until);
            return new(EventSequenceNumber.Unavailable, error, new(Guid.NewGuid(), PartitionDeferralReason.RetryAfter, now, until, batch.FirstOrDefault()?.Context.SequenceNumber ?? EventSequenceNumber.First));
        }

        if (response.StatusCode == HttpStatusCode.Gone)
        {
            deliveryLogger.TargetGone();
            return new(EventSequenceNumber.Unavailable, error) { FailurePolicy = WebhookDeliveryFailurePolicy.RetireWebhook, HttpStatusCode = 410 };
        }

        if ((int)response.StatusCode is >= 400 and < 500 && response.StatusCode is not HttpStatusCode.RequestTimeout and not HttpStatusCode.TooManyRequests)
        {
            deliveryLogger.TargetConfigurationRejected(authority, (int)response.StatusCode);
            return new(EventSequenceNumber.Unavailable, error) { FailurePolicy = WebhookDeliveryFailurePolicy.PauseObserver };
        }

        return new(EventSequenceNumber.Unavailable, error);
    }
}
