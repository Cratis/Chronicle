// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

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

    protected HttpClient Client { get; private set; } = null!;

    async Task Establish()
    {
        var handler = new HttpClientHandler();
        var certificate = X509CertificateLoader.LoadPkcs12FromFile(
            ChronicleOutOfProcessFixtureWithLocalImage.CertificatePath,
            ChronicleOutOfProcessFixtureWithLocalImage.CertPassword);
        handler.ClientCertificates.Add(certificate);
#pragma warning disable MA0039 // Do not write your own certificate validation method
        handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (cert is not null && certificate is not null)
            {
                return cert.GetCertHashString() == certificate.GetCertHashString();
            }

            return sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch;
        };
#pragma warning restore MA0039 // Do not write your own certificate validation method

        Client = CreateClient(
            new()
            {
                BaseAddress = new("https://localhost:8081")
            },
            handler);

        // Add bearer token authentication
        var accessToken = await _fixture.GetAccessToken();
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }
}
