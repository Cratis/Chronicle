// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Api.given;

public class the_client(ChronicleDevelopmentFixture fixture) : the_specification_context(fixture)
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