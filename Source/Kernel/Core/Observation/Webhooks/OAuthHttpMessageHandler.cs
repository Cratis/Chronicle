// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Headers;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Applies a current token to each request and retries a rejected token once.
/// </summary>
/// <param name="client">The transport client, owned by the handler.</param>
/// <param name="authorization">The OAuth configuration.</param>
/// <param name="tokens">The shared token provider.</param>
public class OAuthHttpMessageHandler(HttpClient client, OAuthAuthorization authorization, IOAuthClient tokens) : HttpMessageHandler
{
    /// <summary>
    /// Wraps a configured transport client, preserving its address, timeout and headers.
    /// </summary>
    /// <param name="client">The configured transport.</param>
    /// <param name="authorization">The OAuth configuration.</param>
    /// <param name="tokens">The token provider.</param>
    /// <returns>A client that authorizes every send.</returns>
    public static HttpClient CreateClient(HttpClient client, OAuthAuthorization authorization, IOAuthClient tokens)
    {
#pragma warning disable CA2000 // HttpClient owns the handler via disposeHandler: true; configuration failures dispose the client below.
        var authorized = new HttpClient(new OAuthHttpMessageHandler(client, authorization, tokens), disposeHandler: true);
#pragma warning restore CA2000
        try
        {
            authorized.BaseAddress = client.BaseAddress;
            authorized.Timeout = client.Timeout;
            authorized.DefaultRequestVersion = client.DefaultRequestVersion;
            authorized.DefaultVersionPolicy = client.DefaultVersionPolicy;
            foreach (var header in client.DefaultRequestHeaders)
            {
                authorized.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }

            return authorized;
        }
        catch
        {
            authorized.Dispose();
            throw;
        }
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        var token = await tokens.AcquireToken(authorization, cancellationToken);
        var response = await Send(request, body, token, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        response.Dispose();
        tokens.Invalidate(authorization, token);
        token = await tokens.AcquireToken(authorization, cancellationToken);
        var retried = await Send(request, body, token, cancellationToken);
        if (retried.StatusCode == HttpStatusCode.Unauthorized)
        {
            tokens.Invalidate(authorization, token);
        }
        return retried;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            client.Dispose();
        }
        base.Dispose(disposing);
    }

    async Task<HttpResponseMessage> Send(HttpRequestMessage original, byte[]? body, AccessTokenInfo token, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(original.Method, original.RequestUri)
        {
            Version = original.Version,
            VersionPolicy = original.VersionPolicy
        };
        foreach (var header in original.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        foreach (var option in original.Options)
        {
            request.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }
        if (body is not null)
        {
            request.Content = new ByteArrayContent(body);
            foreach (var header in original.Content!.Headers)
            {
                request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }
}
