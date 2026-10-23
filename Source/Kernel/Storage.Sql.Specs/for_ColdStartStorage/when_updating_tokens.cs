// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Security;
using Cratis.Chronicle.Storage.Sql.Cluster.Security;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_ColdStartStorage;

public class when_updating_tokens : given.a_cold_start_database
{
    int _count;
    string _payload;

    async Task Because()
    {
        var storage = new SqlTokenStorage(_database, new JsonSerializerOptions());
        await Race(() => storage.Update(new Token { Id = "token", Payload = "payload" }));
        _count = await _cluster.Tokens.CountAsync();
        _payload = (await storage.GetById("token"))!.Payload;
    }

    [Fact] void should_keep_one_row() => _count.ShouldEqual(1);
    [Fact] void should_keep_the_token_payload() => _payload.ShouldEqual("payload");
}
