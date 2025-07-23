// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Api.given;

public class a_configured_http_client(ChronicleOutOfProcessFixture fixture) : a_specification_context(fixture)
{
    protected HttpClient Client { get; private set; }

    void Establish()
    {
        Client = CreateClient(new()
        {
            BaseAddress = new("http://localhost:8080")
        });
    }
}
