// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Integration.Api.given;

public class an_http_client : Specification
{
    private readonly ChronicleOutOfProcessFixtureWithLocalImage _fixture;

#pragma warning disable IDE0290 // Use primary constructor
    public an_http_client(ChronicleOutOfProcessFixtureWithLocalImage fixture) : base(fixture)
#pragma warning restore IDE0290 // Use primary constructor
    {
        _fixture = fixture;
    }

    protected HttpClient Client { get; private set; }

    async Task Establish()
    {
        var handler = new HttpClientHandler();
        var certificate = X509CertificateLoader.LoadPkcs12FromFile(
            ChronicleOutOfProcessFixtureWithLocalImage.CertificatePath,
            ChronicleOutOfProcessFixtureWithLocalImage.CertPassword);
        handler.ClientCertificates.Add(certificate);

        // The kernel's test certificate is also the client identity, so it is trusted through the same pin
        // the client uses; its SAN covers localhost, so host name verification still applies.
        var validate = CertificateLoader.CreateServerCertificateValidationCallback(
            skipTlsValidation: false,
            pinnedCertificateHash: certificate.GetCertHashString());
#pragma warning disable MA0039 // Delegates to the client's shared validator rather than writing a new one
        handler.ServerCertificateCustomValidationCallback = (request, cert, chain, sslPolicyErrors) =>
            validate(request, cert, chain, sslPolicyErrors);
#pragma warning restore MA0039 // Delegates to the client's shared validator rather than writing a new one

        Client = CreateClient(
            new()
            {
                BaseAddress = new("https://localhost:35001")
            },
            handler);

        // Add bearer token authentication
        var accessToken = await _fixture.GetAccessToken();
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }
}
