// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.Data.SqlClient;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Cratis.Chronicle.Integration.SqlClustering;

public sealed class SqlClusterDatabase(bool postgres, INetwork network) : IAsyncDisposable
{
    const string Password = "Clustering-5127-Password!";
    const string Database = "chronicle";
    readonly IContainer _container = Build(postgres, network);

    public string KernelConnection => postgres
        ? $"Host=database;Database={Database};Username=postgres;Password={Password}"
        : $"Server=database,1433;Database={Database};User Id=sa;Password={Password};TrustServerCertificate=True";

    public Task Start(CancellationToken token) => _container.StartAsync(token);

    public async Task<IReadOnlyList<MembershipEntry>> Membership(string clusterId, CancellationToken token)
    {
        await using var connection = Connection();
        await connection.OpenAsync(token);
        await using var command = connection.CreateCommand();
        if (postgres)
        {
            command.CommandText = """SELECT "Address", "Generation", "Status" FROM "MembershipEntries" WHERE "ClusterId" = @cluster""";
        }
        else
        {
            command.CommandText = "SELECT [Address], [Generation], [Status] FROM [MembershipEntries] WHERE [ClusterId] = @cluster";
        }

        var parameter = command.CreateParameter();
        parameter.ParameterName = "cluster";
        parameter.Value = clusterId;
        command.Parameters.Add(parameter);
        await using var reader = await command.ExecuteReaderAsync(token);
        var entries = new List<MembershipEntry>();
        while (await reader.ReadAsync(token))
        {
            entries.Add(new(reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2)));
        }

        return entries;
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    static IContainer Build(bool postgres, INetwork network)
    {
        if (postgres)
        {
            return new PostgreSqlBuilder("postgres:16")
                .WithNetwork(network).WithNetworkAliases("database")
                .WithDatabase(Database).WithPassword(Password).Build();
        }

        var builder = new ContainerBuilder(SqlServerContainerImage.Name)
            .WithNetwork(network).WithNetworkAliases("database")
            .WithPortBinding(1433, true)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", Password)
            .WithEnvironment("MSSQL_PID", "Developer")
            .WithCreateParameterModifier(parameters => parameters.HostConfig.Memory = 3L * 1024 * 1024 * 1024)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted(
                "/opt/mssql-tools18/bin/sqlcmd", "-S", "localhost", "-U", "sa", "-P", Password, "-Q", "SELECT 1", "-C"));
        if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64)
        {
            builder = builder.WithCreateParameterModifier(parameters => parameters.Platform = "linux/amd64");
        }

        return builder.Build();
    }

    DbConnection Connection() => postgres
        ? new NpgsqlConnection($"Host={_container.Hostname};Port={_container.GetMappedPublicPort(5432)};Database={Database};Username=postgres;Password={Password}")
        : new SqlConnection($"Server={_container.Hostname},{_container.GetMappedPublicPort(1433)};Database={Database};User Id=sa;Password={Password};TrustServerCertificate=True");
}
