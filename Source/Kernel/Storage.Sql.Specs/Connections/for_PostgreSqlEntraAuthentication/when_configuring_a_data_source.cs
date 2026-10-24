// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_PostgreSqlEntraAuthentication;

public class when_configuring_a_data_source : given.an_authentication_provider
{
    NpgsqlDataSource _source;
    Exception? _exception;

    void Because()
    {
        var builder = new NpgsqlDataSourceBuilder("Host=postgres.example;Username=kernel");
        Authentication.Configure(builder);
        _source = builder.Build();

        // Npgsql refuses a second password provider, proving our data source has one registered without opening a connection.
        builder.UsePeriodicPasswordProvider((_, _) => ValueTask.FromResult("another-password"), TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(1));
        _exception = Catch.Exception(() => builder.Build());
    }

    void Destroy() => _source.Dispose();

    [Fact] void should_register_a_password_provider() => _exception.ShouldBeOfExactType<NotSupportedException>();
}
