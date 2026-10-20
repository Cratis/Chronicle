// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Cratis.DependencyInjection;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represents an implementation of <see cref="IWebhookMediator"/>.
/// </summary>
/// <param name="webhookHttpClientFactory">The <see cref="IWebhookHttpClientFactory"/>.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
[Singleton]
public class WebhookMediator(IWebhookHttpClientFactory webhookHttpClientFactory, JsonSerializerOptions jsonSerializerOptions) : IWebhookMediator
{
    /// <inheritdoc/>
    public async Task<Catch> OnNext(EventStoreName? eventStore, WebhookTarget webhookTarget, Key partition, IEnumerable<AppendedEvent> events, string? accessToken = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeout is { } duration && duration > TimeSpan.Zero)
        {
            deadline.CancelAfter(duration);
        }

        try
        {
            var httpClient = webhookHttpClientFactory.Create(webhookTarget, accessToken);
            var batch = events.ToArray();
            using var request = new HttpRequestMessage(HttpMethod.Post, string.Empty)
            {
                Content = JsonContent.Create(new EventsToObserve(partition.ToString(), batch), options: jsonSerializerOptions)
            };
            if (eventStore is not null) request.Options.Set(WebhookRequestOptions.EventStore, eventStore);
            using var response = await httpClient.SendAsync(request, deadline.Token);
            if (!response.IsSuccessStatusCode)
            {
                return Catch.Failed(new WebhookDeliveryFailed(webhookTarget.Url, response.StatusCode, response.ReasonPhrase));
            }

            return Catch.Success();
        }
        catch (Exception e)
        {
            return Catch.Failed(e);
        }
    }
}
