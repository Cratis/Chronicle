// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_ResolvedIdentityInterceptor;

public class when_preparing_a_connection : Specification
{
    ICachingConnectionResolver _resolver;
    ConnectionRequest _request;
    SqlConnection _connection;
    SqlConnectionStringBuilder _result;

    void Establish()
    {
        _request = new(WellKnownConnectionStorage.SqlServer, ConnectionPurpose.Runtime, new Dictionary<string, string>());
        _resolver = Substitute.For<ICachingConnectionResolver>();
        _resolver.Resolve(_request, Arg.Any<CancellationToken>()).Returns(new ResolvedConnection("Server=elsewhere", "elsewhere", Identity: new(AccessToken: "the-token")));
        _connection = new("Server=sql.example;Database=tenant-acme;Encrypt=true");
    }

    async Task Because()
    {
        await new ResolvedIdentityInterceptor(_resolver, _request).Prepare(_connection);
        _result = new(_connection.ConnectionString);
    }

    void Destroy() => _connection.Dispose();

    [Fact] void should_apply_the_access_token() => _connection.AccessToken.ShouldEqual("the-token");
    [Fact] void should_keep_the_server_the_options_were_built_for() => _result.DataSource.ShouldEqual("sql.example");
    [Fact] void should_keep_the_database_the_options_were_built_for() => _result.InitialCatalog.ShouldEqual("tenant-acme");
}
