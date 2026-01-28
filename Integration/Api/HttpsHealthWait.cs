// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.Integration.Api;

/// <summary>
/// Represents a wait strategy that waits for an HTTPS health endpoint to be available.
/// </summary>
/// <param name="containerPort">The container port to check.</param>
/// <param name="path">The health check path.</param>
public sealed class HttpsHealthWait(ushort containerPort, string path = "/health") : IWaitUntil
{
    readonly ushort _containerPort = containerPort;
    readonly string _path = path.StartsWith('/') ? path : "/" + path;

    /// <inheritdoc/>
    public async Task<bool> UntilAsync(IContainer container)
    {
        var httpsPortOnHost = container.GetMappedPublicPort(_containerPort);

        var uri = new UriBuilder(Uri.UriSchemeHttps, container.Hostname, httpsPortOnHost, _path).Uri;

#pragma warning disable MA0039 // Do not write your own certificate validation method
        using var handler = new HttpClientHandler
        {
            // Accept untrusted/self-signed certs (test-only).
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
#pragma warning restore MA0039 // Do not write your own certificate validation method

#pragma warning disable CA5400 // HttpClient may be created without enabling CheckCertificateRevocationList
        using var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(2)
        };
#pragma warning restore CA5400 // HttpClient may be created without enabling CheckCertificateRevocationList

        try
        {
            using var resp = await client.GetAsync(uri);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
