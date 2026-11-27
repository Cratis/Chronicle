// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Text.Json;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.SqlClustering;

internal sealed class ClusterReconnectDiagnostics(SqlCluster cluster, IEventStore store, string probeSource)
{
    static readonly TimeSpan _readTimeout = TimeSpan.FromSeconds(5);

    internal async Task Dump(IReactorHandler reactor, TextWriter output, DateTimeOffset failedAt)
    {
        await Capture(async () =>
        {
            await output.WriteLineAsync($"Reconnect evidence: DeadlineFailureAt={failedAt:O}, CapturedAt={DateTimeOffset.UtcNow:O}, ProbeSource={probeSource}; source=public client API and isolated SQL fixture. Setup-failure flag and live target/known-client membership are unavailable through this API; IsSubscribed is not setup health.");
            await DumpObserver(reactor, output);
            var reducer = store.Reducers.GetHandlerFor(typeof(ClusterReconnectReducer));
            await DumpObserver(reducer, output);
            await DumpStorage([reactor.Id.Value, reducer.Id.Value], output);
        });
    }

    /// <summary>
    /// Capture evidence without letting failed or late reads replace the frozen timeout.
    /// </summary>
    /// <param name="capture">The observational evidence capture.</param>
    /// <returns>The asynchronous capture.</returns>
    internal static async Task Capture(Func<Task> capture)
    {
        try
        {
            await capture().WaitAsync(TimeSpan.FromSeconds(20));
        }
        catch
        {
            // An unavailable writer or dependency cannot replace the original reconnect failure.
        }
    }

    static Task DumpObserver(IReactorHandler reactor, TextWriter output) => DumpObserver(
        async () => ClusterLoadDiagnostics.DescribeState(await reactor.GetState()),
        reactor.GetFailedPartitions,
        output);

    static Task DumpObserver(IReducerHandler reducer, TextWriter output) => DumpObserver(
        async () => DescribeState(reducer.Id.Value, await reducer.GetState()),
        reducer.GetFailedPartitions,
        output);

    static async Task DumpObserver(Func<Task<string>> state, Func<Task<IEnumerable<FailedPartition>>> failures, TextWriter output)
    {
        await ReadEvidence(async () => await output.WriteLineAsync(await state().WaitAsync(_readTimeout)), output);
        await ReadEvidence(
            async () =>
            {
                var partitions = (await failures().WaitAsync(_readTimeout)).ToArray();
                await output.WriteLineAsync($"Failed partitions: count={partitions.Length}, first 20; next retry time unavailable through client API");
                foreach (var partition in partitions.Take(20))
                {
                    await output.WriteLineAsync(DescribeFailure(partition));
                }
            },
            output);
    }

    internal static string DescribeState(string observer, ReducerState state) =>
        $"Observer {observer}: RunningState={state.RunningState}, IsSubscribed={state.IsSubscribed}, LastHandledEventSequenceNumber={state.LastHandledEventSequenceNumber}, NextEventSequenceNumber={state.NextEventSequenceNumber}, TailEventSequenceNumber={state.TailEventSequenceNumber}";

    internal static string DescribeFailure(FailedPartition partition) =>
        $"{ClusterLoadDiagnostics.DescribeFailedPartition(partition)}, Id={partition.Id}, Attempts={partition.Attempts.Count()}, LastAttemptAt={partition.Attempts.LastOrDefault()?.Occurred:O}, NextRetryAt=unavailable";

    static async Task ReadEvidence(Func<Task> read, TextWriter output)
    {
        try
        {
            await read().WaitAsync(_readTimeout);
        }
        catch (Exception exception)
        {
            await output.WriteLineAsync($"Reconnect evidence unavailable: {exception.GetType().Name}");
        }
    }

    async Task DumpStorage(string[] observers, TextWriter output)
    {
        await ReadEvidence(
            async () =>
            {
                await using var connection = cluster.CreateDiagnosticConnection();
                await connection.OpenAsync();
                await DumpReminders(connection, observers, output);
            },
            output);
        await ReadEvidence(
            async () =>
            {
                await using var connection = cluster.CreateDiagnosticConnection();
                await connection.OpenAsync();
                var database = await NamespaceDatabase(connection);
                if (database is null)
                {
                    await output.WriteLineAsync("Durable target evidence unavailable: namespace database binding absent");
                    return;
                }
                await connection.ChangeDatabaseAsync(database);
                await DumpTargets(connection, observers, output);
            },
            output);
    }

    static async Task DumpReminders(DbConnection connection, string[] observers, TextWriter output)
    {
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 5;
        command.CommandText = """SELECT "GrainId", "GrainHash", "StartAt", "Period" FROM "Reminders" WHERE "ReminderName" = @name AND ("GrainId" LIKE @reactor OR "GrainId" LIKE @reducer)""";

        // SQL Server's default QUOTED_IDENTIFIER is ON, so the same identifier quoting works for both providers.
        AddParameter(command, "name", "CheckSubscription");
        AddParameter(command, "reactor", $"%{observers[0]}%");
        AddParameter(command, "reducer", $"%{observers[1]}%");
        await using var rows = await command.ExecuteReaderAsync();
        var count = 0;
        while (count < 20 && await rows.ReadAsync())
        {
            await output.WriteLineAsync($"CheckSubscription reminder (isolated SQL): GrainId={rows.GetString(0)}, GrainHash={rows.GetInt64(1)}, StartAt={DateTime.FromBinary(rows.GetInt64(2)):O}, PeriodMs={rows.GetInt64(3)}");
            count++;
        }
        await output.WriteLineAsync($"CheckSubscription reminder rows shown={count}, limit=20; absence is not proof that a client is unknown");
    }

    async Task<string?> NamespaceDatabase(DbConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 5;
        command.CommandText = """SELECT "PhysicalName" FROM "DatabaseNames" WHERE "Kind" = @kind AND "EventStore" = @store AND "Namespace" = @namespace""";
        AddParameter(command, "kind", 2); // DatabaseKind.Namespace in the fixture's catalog.
        AddParameter(command, "store", store.Name.Value);
        AddParameter(command, "namespace", store.Namespace.Value);
        return await command.ExecuteScalarAsync() as string;
    }

    static async Task DumpTargets(DbConnection connection, string[] observers, TextWriter output)
    {
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 5;
        command.CommandText = """SELECT "Id", "DurableSubscription" FROM "Observers" WHERE "Id" = @reactor OR "Id" = @reducer""";
        AddParameter(command, "reactor", observers[0]);
        AddParameter(command, "reducer", observers[1]);
        await using var rows = await command.ExecuteReaderAsync();
        while (await rows.ReadAsync())
        {
            var targets = await rows.IsDBNullAsync(1) ? "absent" : DescribeTargets(rows.GetString(1));
            await output.WriteLineAsync($"Observer {rows.GetString(0)} durable target evidence (SQL, not live membership/health): {targets}");
        }
    }

    internal static string DescribeTargets(string json)
    {
        using var document = JsonDocument.Parse(json);
        return string.Join("; ", document.RootElement.GetProperty("Targets").EnumerateArray().Take(20)
            .Select(target => $"ConnectionId={target.GetProperty("ConnectionId").GetString()}, Silo={target.GetProperty("Silo").GetString()}"));
    }

    static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
