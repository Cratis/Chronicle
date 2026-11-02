// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider;

public class CapturingTokenEndpointHandler : HttpMessageHandler
{
    public List<CapturedRequest> Requests { get; } = [];
    public string Token { get; set; } = "the-token";
    public int ExpiresIn { get; set; } = 3600;
    public bool FailDiscovery { get; set; }
    public Exception? DiscoveryException { get; set; }
    public Action? OnDiscovery { get; set; }
    public HttpStatusCode DiscoveryStatus { get; set; } = HttpStatusCode.OK;
    public HttpStatusCode TokenStatus { get; set; } = HttpStatusCode.OK;
    public string? TokenErrorBody { get; set; }
    public string DiscoveryDocument { get; set; } = "{\"token_endpoint\":\"https://idp.example.com/realms/chronicle/protocol/openid-connect/token\"}";

    public IEnumerable<CapturedRequest> DiscoveryRequests => Requests.Where(IsDiscovery);
    public IEnumerable<CapturedRequest> TokenRequests => Requests.Where(_ => !IsDiscovery(_));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var form = new Dictionary<string, string>();
        if (request.Content is not null)
        {
            var body = await request.Content.ReadAsStringAsync(cancellationToken);
            foreach (var pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                form[Uri.UnescapeDataString(parts[0].Replace('+', ' '))] = Uri.UnescapeDataString((parts.Length > 1 ? parts[1] : string.Empty).Replace('+', ' '));
            }
        }

        var captured = new CapturedRequest(request.RequestUri!, request.Method, form);
        Requests.Add(captured);

        if (IsDiscovery(captured))
        {
            OnDiscovery?.Invoke();
            if (DiscoveryException is not null)
            {
                throw DiscoveryException;
            }

            if (FailDiscovery)
            {
                throw new HttpRequestException("Simulated discovery failure");
            }

            return IsRedirect(DiscoveryStatus) ? Redirect(DiscoveryStatus) : new HttpResponseMessage(DiscoveryStatus) { Content = new StringContent(DiscoveryDocument) };
        }

        if (IsRedirect(TokenStatus))
        {
            return Redirect(TokenStatus);
        }

        if (TokenErrorBody is not null)
        {
            return new HttpResponseMessage(TokenStatus == HttpStatusCode.OK ? HttpStatusCode.BadRequest : TokenStatus) { Content = new StringContent(TokenErrorBody) };
        }

        return new HttpResponseMessage(TokenStatus)
        {
            Content = new StringContent($"{{\"access_token\":\"{Token}\",\"expires_in\":{ExpiresIn}}}")
        };
    }

    static bool IsRedirect(HttpStatusCode status) => (int)status is >= 300 and < 400;

    static HttpResponseMessage Redirect(HttpStatusCode status)
    {
        var response = new HttpResponseMessage(status);
        response.Headers.Location = new Uri("https://elsewhere.example.net/collect");
        return response;
    }

    static bool IsDiscovery(CapturedRequest request) => request.Uri.AbsolutePath.EndsWith("/.well-known/openid-configuration", StringComparison.Ordinal);
}
