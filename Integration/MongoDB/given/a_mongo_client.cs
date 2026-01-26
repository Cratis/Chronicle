// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.MongoDB.Integration.given;

public class a_mongo_client(ChronicleInProcessFixture fixture) : Specification(fixture), IDisposable
{
    protected MongoClient _client = default!;

    void Establish()
    {
        _client = new MongoClient($"mongodb://localhost:{XUnit.Integration.ChronicleFixture.MongoDBPort}/?directConnection=true");
    }

    public override void Dispose()
    {
        _client?.Dispose();
        _client = null!;
        base.Dispose();
    }
}
