// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Api.given;

public class an_http_client(ChronicleOutOfProcessFixtureWithLocalImage fixture) : Specification(fixture)
{
    protected HttpClient Client { get; private set; } = null!;

    void Establish()
    {
        Client = CreateClient(new()
        {
            BaseAddress = new("http://localhost:8080")
        });
    }
}
