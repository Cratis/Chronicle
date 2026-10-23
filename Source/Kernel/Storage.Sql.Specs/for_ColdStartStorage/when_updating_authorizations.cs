// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Security;
using Cratis.Chronicle.Storage.Sql.Cluster.Security;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_ColdStartStorage;

public class when_updating_authorizations : given.a_cold_start_database
{
    int _count;
    Authorization? _saved;

    async Task Because()
    {
        var storage = new SqlAuthorizationStorage(_database, new JsonSerializerOptions());
        var value = new Authorization("00000000-0000-0000-0000-000000000001", null, null, null, null, [], null, []);
        await Race(() => storage.Update(value));
        _count = await _cluster.Authorizations.CountAsync();
        _saved = await storage.GetById("00000000-0000-0000-0000-000000000001");
    }

    [Fact] void should_keep_one_row() => _count.ShouldEqual(1);
    [Fact] void should_keep_the_requested_identity() => _saved!.Id.ToString().ShouldEqual("00000000-0000-0000-0000-000000000001");
}
