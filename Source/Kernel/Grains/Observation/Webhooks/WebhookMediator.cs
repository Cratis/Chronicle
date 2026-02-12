// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.DependencyInjection;
using Cratis.Monads;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Represents an implementation of <see cref="IWebhookMediator"/>.
/// </summary>
/// <param name="webhookHttpClientFactory">The <see cref="IWebhookHttpClientFactory"/>.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
[Singleton]
public class WebhookMediator(IWebhookHttpClientFactory webhookHttpClientFactory, JsonSerializerOptions jsonSerializerOptions) : IWebhookMediator
{
    /// <inheritdoc/>
    public async Task<Catch> OnNext(WebhookTarget webhookTarget, Key partition, IEnumerable<AppendedEvent> events, string? accessToken = null, TimeSpan? timeout = null)
    {
        try
        {
            var httpClient = webhookHttpClientFactory.Create(webhookTarget, accessToken);
            await httpClient.PostAsJsonAsync(
                string.Empty,
                new EventsToObserve(partition.ToString(), events.ToArray()),
                jsonSerializerOptions);
            return Catch.Success();
        }
        catch (Exception e)
        {
            return Catch.Failed(e);
        }
    }
}
