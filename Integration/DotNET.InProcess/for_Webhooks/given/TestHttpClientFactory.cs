// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.InProcess.Integration.for_Webhooks.given;

public class TestHttpClientFactory(HttpClient testClient) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => testClient;
}