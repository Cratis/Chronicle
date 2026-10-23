// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represents an implementation of <see cref="IWebhookHttpClientFactory"/>.
/// </summary>
/// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
/// <param name="oAuthClient">The shared OAuth token provider.</param>
/// <param name="managedIdentityTokens">The managed identity token provider.</param>
/// <param name="targetPolicy">The operator approval policy. Without it managed identity delivery is refused.</param>
public class WebhookHttpClientFactory(IHttpClientFactory httpClientFactory, IOAuthClient oAuthClient, IManagedIdentityTokens managedIdentityTokens, IWebhookTargetPolicy? targetPolicy = null) : IWebhookHttpClientFactory
{
    /// <summary>
    /// The name of the <see cref="HttpClient"/> to use.
    /// </summary>
    public const string HttpClientName = "webhook";

    /// <summary>
    /// Initializes a factory for hosts without managed identity token acquisition.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP transport factory.</param>
    /// <param name="oAuthClient">The shared OAuth token provider.</param>
    public WebhookHttpClientFactory(IHttpClientFactory httpClientFactory, IOAuthClient oAuthClient)
        : this(httpClientFactory, oAuthClient, new ManagedIdentityTokens())
    {
    }

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
            oAuth => client = OAuthHttpMessageHandler.CreateClient(client, oAuth, oAuthClient),
            none => { },
            managedIdentity => client = TokenHttpMessageHandler.CreateClient(
                client,
                (eventStore, cancellation) =>
                {
                    if (targetPolicy?.CheckManagedIdentity(managedIdentity, webhookTarget.Url, eventStore).IsAllowed != true)
                    {
                        throw new ManagedIdentityWebhookNotAllowed();
                    }
                    return managedIdentityTokens.AcquireToken(managedIdentity, cancellation);
                },
                token => managedIdentityTokens.Invalidate(managedIdentity, token)));

        foreach (var header in webhookTarget.Headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        return client;
    }
}