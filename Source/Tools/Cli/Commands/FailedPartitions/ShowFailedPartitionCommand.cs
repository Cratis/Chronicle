// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Spectre.Console;

namespace Cratis.Chronicle.Cli.Commands.FailedPartitions;

/// <summary>
/// Shows detailed information about a specific failed partition, including all retry attempts and stack traces.
/// </summary>
public class ShowFailedPartitionCommand : ChronicleCommand<ShowFailedPartitionSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IChronicleClient client, ShowFailedPartitionSettings settings, string format)
    {
        var eventStore = await client.GetEventStore(settings.ResolveEventStore());
        var services = GetServices(eventStore);

        var failedPartitions = await services.FailedPartitions.GetFailedPartitions(new GetFailedPartitionsRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            ObserverId = settings.ObserverId
        });

        var match = failedPartitions.FirstOrDefault(fp =>
            string.Equals(fp.Partition, settings.Partition, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            OutputFormatter.WriteError(
                format,
                $"No failed partition '{settings.Partition}' found for observer '{settings.ObserverId}'",
                "Use 'cratis failed-partitions list' to see all failed partitions");
            return ExitCodes.NotFound;
        }

        var attempts = (match.Attempts ?? []).ToList();

        OutputFormatter.WriteObject(
            format,
            new
            {
                match.Id,
                match.ObserverId,
                match.Partition,
                AttemptCount = attempts.Count,
                Attempts = attempts.Select(a => new
                {
                    Occurred = a.Occurred?.ToString() ?? string.Empty,
                    a.SequenceNumber,
                    Messages = (a.Messages ?? []).ToArray(),
                    a.StackTrace
                }).ToArray()
            },
            data =>
            {
                AnsiConsole.MarkupLine($"[bold]FailedPartition:[/] {data.Id}");
                AnsiConsole.MarkupLine($"[bold]Observer:[/]        {data.ObserverId.EscapeMarkup()}");
                AnsiConsole.MarkupLine($"[bold]Partition:[/]       {data.Partition.EscapeMarkup()}");
                AnsiConsole.MarkupLine($"[bold]Attempts:[/]        {data.AttemptCount}");
                AnsiConsole.WriteLine();

                foreach (var attempt in data.Attempts)
                {
                    AnsiConsole.MarkupLine($"  [yellow]--- Attempt at {attempt.Occurred.EscapeMarkup()} (Seq# {attempt.SequenceNumber}) ---[/]");
                    foreach (var message in attempt.Messages)
                    {
                        AnsiConsole.MarkupLine($"  [red]{message.EscapeMarkup()}[/]");
                    }

                    if (!string.IsNullOrWhiteSpace(attempt.StackTrace))
                    {
                        AnsiConsole.MarkupLine("[dim]  StackTrace:[/]");
                        AnsiConsole.WriteLine($"  {attempt.StackTrace}");
                    }

                    AnsiConsole.WriteLine();
                }
            });

        return ExitCodes.Success;
    }
}
