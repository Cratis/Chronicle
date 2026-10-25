// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.Connections;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

public sealed class MembershipHarness(Func<Task<string>> createDatabase, Func<string, Task> dropDatabase, bool sqlServer) : IAsyncDisposable
{
    string _connectionString;
    string _adminConnectionString;
    string? _login;
    ResolvedIdentityInterceptor? _identityInterceptor;
    public int IdentityResolutions { get; private set; }

    public async Task Initialize(bool nonDefaultSchema)
    {
        _connectionString = await createDatabase();
        if (nonDefaultSchema)
        {
            await UseNonDefaultSchema();
        }

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public ClusterDbContext CreateContext(params IInterceptor[] interceptors)
    {
        var builder = sqlServer
            ? new DbContextOptionsBuilder<ClusterDbContext>().EnableServiceProviderCaching(false).AddConceptAsSupport()
            : new DbContextOptionsBuilder<ClusterDbContext>(SpecDbContextOptions.Kernel<ClusterDbContext>(_connectionString));
        builder.AddInterceptors(interceptors);
        if (sqlServer)
        {
            builder.UseSqlServer(_connectionString, options => options.EnableRetryOnFailure(2, TimeSpan.Zero, null));
        }
        else
        {
            builder.UseNpgsql(_connectionString, options => options.EnableRetryOnFailure(2, TimeSpan.Zero, null));
        }

        if (_identityInterceptor is not null)
        {
            builder.AddInterceptors(_identityInterceptor);
        }

        return new ClusterDbContext(builder.Options);
    }

    async Task UseNonDefaultSchema()
    {
        _adminConnectionString = _connectionString;
        if (sqlServer)
        {
            _login = "membership_schema_spec";
            const string password = "Schema-Spec-Password!123";
            await using var connection = new SqlConnection(_adminConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE SCHEMA membership";
            await command.ExecuteNonQueryAsync();
            command.CommandText = "CREATE LOGIN [membership_schema_spec] WITH PASSWORD = 'Schema-Spec-Password!123'; CREATE USER [membership_schema_spec] FOR LOGIN [membership_schema_spec] WITH DEFAULT_SCHEMA = membership; ALTER ROLE db_owner ADD MEMBER [membership_schema_spec];";
            await command.ExecuteNonQueryAsync();
            _connectionString = new SqlConnectionStringBuilder(_connectionString) { UserID = _login, Password = password }.ConnectionString;
        }
        else
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE SCHEMA membership";
            await command.ExecuteNonQueryAsync();
            _connectionString = new NpgsqlConnectionStringBuilder(_connectionString) { SearchPath = "membership" }.ConnectionString;
        }
    }

    public void UseResolvedIdentity()
    {
        var resolver = Substitute.For<ICachingConnectionResolver>();
        var request = new ConnectionRequest(
            sqlServer ? WellKnownConnectionStorage.SqlServer : WellKnownConnectionStorage.PostgreSql,
            ConnectionPurpose.Runtime,
            new Dictionary<string, string>());
        string username;
        string password;
        _adminConnectionString = _connectionString;
        if (sqlServer)
        {
            var builder = new SqlConnectionStringBuilder(_connectionString);
            username = builder.UserID;
            password = builder.Password;
            builder.Password = "wrong-configured-password";
            _connectionString = builder.ConnectionString;
        }
        else
        {
            var builder = new NpgsqlConnectionStringBuilder(_connectionString);
            username = builder.Username!;
            password = builder.Password!;
            builder.Password = "wrong-configured-password";
            _connectionString = builder.ConnectionString;
        }

        resolver.Resolve(request, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            IdentityResolutions++;
            return Task.FromResult(new ResolvedConnection(Identity: new(username, password)));
        });
        _identityInterceptor = new ResolvedIdentityInterceptor(resolver, request);
    }

    public async ValueTask DisposeAsync()
    {
        if (_login is not null)
        {
            SqlConnection.ClearAllPools();
            await using var connection = new SqlConnection(_adminConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "DROP LOGIN [membership_schema_spec]";
            await command.ExecuteNonQueryAsync();
        }

        await dropDatabase(_adminConnectionString ?? _connectionString);
    }
}
