// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Security;
using Cratis.Chronicle.Storage.Sql.Cluster.Security;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_ColdStartStorage;

public class when_updating_scopes : given.a_cold_start_database
{
    int _count;
    Scope? _saved;

    async Task Because()
    {
        var storage = new SqlScopeStorage(_database, new JsonSerializerOptions());
        var value = new Scope("id", "name", "display", "description", [], []);
        await Race(() => storage.Update(value));
        _count = await _cluster.Scopes.CountAsync();
        _saved = await storage.GetById("id");
    }

    [Fact] void should_keep_one_row() => _count.ShouldEqual(1);
    [Fact] void should_keep_the_requested_identity() => _saved!.Id.ShouldEqual("id");
}
