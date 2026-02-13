// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Represents an implementation of <see cref="IWebhookHttpClientFactory"/>.
/// </summary>
/// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
public class WebhookHttpClientFactory(IHttpClientFactory httpClientFactory) : IWebhookHttpClientFactory
{
    /// <summary>
    /// The name of the <see cref="HttpClient"/> to use.
    /// </summary>
    public const string HttpClientName = "webhook";

    /// <inheritdoc/>
    public HttpClient Create(WebhookTarget webhookTarget, string? accessToken = null)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);

        if (Uri.TryCreate(webhookTarget.Url, UriKind.Absolute, out var uri))
        {
            client.BaseAddress = uri;
        }

        webhookTarget.Authorization.Switch(
            basic =>
            {
                var value = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{basic.Username}:{basic.Password}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", value);
            },
            bearer => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer.Token),
            oAuth =>
            {
                if (!string.IsNullOrEmpty(accessToken))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
            },
            none => { });

        foreach (var header in webhookTarget.Headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        return client;
    }
}