// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_ResolvedIdentityInterceptor;

public class with_a_postgresql_resolver_token : Specification
{
    ICachingConnectionResolver _resolver;
    ConnectionRequest _request;
    NpgsqlConnection _connection;
    NpgsqlConnectionStringBuilder _settings;

    void Establish()
    {
        _request = new(WellKnownConnectionStorage.PostgreSql, ConnectionPurpose.Runtime, new Dictionary<string, string>());
        _resolver = Substitute.For<ICachingConnectionResolver>();
        _resolver.Resolve(_request, Arg.Any<CancellationToken>()).Returns(new ResolvedConnection(Identity: new(UserName: "resolved-user", AccessToken: "resolver-token")));
        _connection = new("Host=postgres.example;Database=chronicle;SSL Mode=VerifyFull");
    }

    async Task Because()
    {
        await new ResolvedIdentityInterceptor(_resolver, _request).Prepare(_connection);
        _settings = new(_connection.ConnectionString);
    }

    void Destroy() => _connection.Dispose();

    [Fact] void should_use_the_resolved_token_as_the_password() => _settings.Password.ShouldEqual("resolver-token");
    [Fact] void should_use_the_resolved_user() => _settings.Username.ShouldEqual("resolved-user");
    [Fact] void should_keep_the_database() => _settings.Database.ShouldEqual("chronicle");
}
