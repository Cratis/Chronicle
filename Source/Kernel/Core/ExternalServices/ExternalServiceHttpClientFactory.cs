// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Represents an implementation of <see cref="IExternalServiceHttpClientFactory"/>.
/// </summary>
/// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
/// <param name="oAuthClient">The <see cref="IOAuthClient"/> for acquiring OAuth tokens.</param>
[Singleton]
public class ExternalServiceHttpClientFactory(
    IHttpClientFactory httpClientFactory,
    IOAuthClient oAuthClient) : IExternalServiceHttpClientFactory
{
    /// <summary>
    /// The name of the <see cref="HttpClient"/> to use.
    /// </summary>
    public const string HttpClientName = "external-service";

    /// <inheritdoc/>
    public async Task<HttpClient> Create(ExternalServiceDefinition externalService)
    {
        if (externalService.Endpoint.Http is not { } http)
        {
            throw new ExternalServiceIsNotAnHttpService(externalService.Name);
        }

        var client = httpClientFactory.CreateClient(HttpClientName);

        if (Uri.TryCreate(http.Url, UriKind.Absolute, out var uri))
        {
            client.BaseAddress = uri;
        }

        await http.Authorization.Match(
            basic =>
            {
                var value = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{basic.Username}:{basic.Password}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", value);
                return Task.CompletedTask;
            },
            bearer =>
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer.Token);
                return Task.CompletedTask;
            },
            async oAuth =>
            {
                var accessToken = await oAuthClient.AcquireToken(oAuth);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.AccessToken);
            },
            _ => Task.CompletedTask);

        foreach (var header in http.Headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        return client;
    }
}
