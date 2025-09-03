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
    public HttpClient Create(WebhookDefinition definition)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        var webhookTarget = definition.Target;
        if (Uri.TryCreate(webhookTarget.Url, UriKind.Absolute, out var uri))
        {
            client.BaseAddress = new Uri(uri.GetLeftPart(UriPartial.Authority));
        }

        switch (webhookTarget.Authentication)
        {
            case AuthenticationType.Basic when webhookTarget.Username is not null && webhookTarget.Password is not null:
                var basic = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{webhookTarget.Username}:{webhookTarget.Password}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
                break;

            case AuthenticationType.Bearer when webhookTarget.BearerToken is not null:
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", webhookTarget.BearerToken);
                break;
        }

        foreach (var header in webhookTarget.Headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        return client;
    }
}