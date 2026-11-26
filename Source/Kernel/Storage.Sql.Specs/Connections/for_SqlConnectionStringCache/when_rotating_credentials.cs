// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_SqlConnectionStringCache;

public class when_rotating_credentials : given.a_cache
{
    string _configured;
    string _resolved;
    string _restored;

    void Establish() => Normalize(_input);

    void Because()
    {
        _configured = Normalize(_input with { ConnectionString = "Host=pg.example;Database=chronicle;Username=rotated;Password=new-configured" });
        _resolved = Normalize(_input with { PlacementConnectionString = "Host=pg.example;Database=chronicle;Username=resolved;Password=new-resolved" });
        _restored = Normalize(_input);
    }

    [Fact] void should_use_the_new_configured_password() => new NpgsqlConnectionStringBuilder(_configured).Password.ShouldEqual("new-configured");
    [Fact] void should_use_the_new_configured_user() => new NpgsqlConnectionStringBuilder(_configured).Username.ShouldEqual("rotated");
    [Fact] void should_use_the_new_resolved_password() => new NpgsqlConnectionStringBuilder(_resolved).Password.ShouldEqual("new-resolved");
    [Fact] void should_use_the_new_resolved_user() => new NpgsqlConnectionStringBuilder(_resolved).Username.ShouldEqual("resolved");
    [Fact] void should_restore_the_current_inputs_after_other_credentials() => new NpgsqlConnectionStringBuilder(_restored).Password.ShouldEqual("initial");
}
