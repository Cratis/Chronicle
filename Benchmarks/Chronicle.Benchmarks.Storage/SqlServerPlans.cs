// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// Replays captured fixture SELECTs with actual SQL Server plans and server statistics.
/// </summary>
internal static class SqlServerPlans
{
    /// <summary>
    /// Drains every result set so the actual showplan and trailing info messages arrive.
    /// </summary>
    /// <param name="connectionString">The fixture database connection.</param>
    /// <param name="query">The captured SELECT.</param>
    /// <param name="rawPlanPath">Optional destination for the unabridged XML.</param>
    /// <returns>The operator summary and STATISTICS IO/TIME messages, or a capture error.</returns>
    [SuppressMessage("Security", "CA2100", Justification = "Only captured EF-generated fixture SELECTs are replayed; values remain parameterized.")]
    public static async Task<string> Capture(string connectionString, CapturedQuery query, string? rawPlanPath)
    {
        var messages = new List<string>();
        try
        {
            await using var connection = new SqlConnection(connectionString);
            connection.InfoMessage += (_, info) => messages.Add(info.Message);
            await connection.OpenAsync(RunBudget.Current.Token);
            await using var command = connection.CreateCommand();
            command.CommandText = "SET STATISTICS XML ON; SET STATISTICS IO, TIME ON;\n" + query.Sql;
            command.CommandTimeout = 30;
            foreach (var parameter in query.Parameters) SqlPlanQueries.AddParameter(command, parameter);
            await using var reader = await command.ExecuteReaderAsync(RunBudget.Current.Token);
            var plans = new List<string>();
            do
            {
                var isPlan = reader.FieldCount == 1 && reader.GetName(0) == "Microsoft SQL Server 2005 XML Showplan";
                while (await reader.ReadAsync(RunBudget.Current.Token))
                {
                    if (isPlan) plans.Add(reader.GetString(0));
                }
            }
            while (await reader.NextResultAsync(RunBudget.Current.Token));

            var xml = Combine(plans);
            if (rawPlanPath is not null) await File.WriteAllTextAsync(rawPlanPath, xml, RunBudget.Current.Token);
            var raw = rawPlanPath is null ? string.Empty : $"\nRaw showplan: {Path.GetFileName(rawPlanPath)}\n";
            return SqlServerPlanSummary.Format(xml) + raw + "\nSTATISTICS IO/TIME (logical reads per table; CPU/elapsed in ms):\n" + string.Join('\n', messages);
        }
        catch (Exception error)
        {
            return $"not captured: {error.Message}\n{string.Join('\n', messages)}";
        }
    }

    static string Combine(IReadOnlyList<string> plans) => plans.Count switch
    {
        0 => throw new InvalidOperationException("SQL Server returned no actual showplan XML."),
        1 => plans[0],
        _ => new XElement("ShowPlans", plans.Select(XElement.Parse)).ToString()
    };
}
