// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Waits for an HTTPS health endpoint to become available.
/// </summary>
/// <param name="containerPort">The container port to probe.</param>
/// <param name="path">The health path to request.</param>
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
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
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
            using var response = await client.GetAsync(uri);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
