// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_PostgreSqlEntraAuthentication;

public class when_preparing_a_raw_connection : given.an_authentication_provider
{
    NpgsqlConnection _connection;
    NpgsqlConnectionStringBuilder _settings;

    void Establish() => _connection = new("Host=postgres.example;Database=chronicle;Username=kernel;SSL Mode=VerifyFull");

    async Task Because()
    {
        await Authentication.Prepare(_connection);
        _settings = new(_connection.ConnectionString);
    }

    void Destroy() => _connection.Dispose();

    [Fact] void should_use_the_token_as_the_password() => _settings.Password.ShouldEqual($"token-{Now:O}");
    [Fact] void should_preserve_the_database() => _settings.Database.ShouldEqual("chronicle");
    [Fact] void should_preserve_the_user() => _settings.Username.ShouldEqual("kernel");
    [Fact] void should_preserve_tls_verification() => _settings.SslMode.ShouldEqual(SslMode.VerifyFull);
}
