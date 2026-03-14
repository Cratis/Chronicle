// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Spectre.Console;

namespace Cratis.Chronicle.Cli.Commands.Projections;

/// <summary>
/// Shows the declaration of a specific projection.
/// </summary>
public class ShowProjectionCommand : ChronicleCommand<ShowProjectionSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, ShowProjectionSettings settings, string format)
    {
        var declarations = await services.Projections.GetAllDeclarations(new GetAllDeclarationsRequest
        {
            EventStore = settings.ResolveEventStore()
        });

        var match = declarations.FirstOrDefault(d => string.Equals(d.Identifier, settings.Identifier, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            OutputFormatter.WriteError(format, $"Projection '{settings.Identifier}' not found", "Use 'cratis projections list' to see available projections");
            return ExitCodes.NotFound;
        }

        OutputFormatter.WriteObject(
            format,
            new
            {
                match.Identifier,
                match.ContainerName,
                match.Declaration
            },
            data =>
            {
                AnsiConsole.MarkupLine($"[bold]Projection:[/] {data.Identifier.EscapeMarkup()}");
                AnsiConsole.MarkupLine($"[bold]Container:[/]  {data.ContainerName.EscapeMarkup()}");
                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine(data.Declaration);
            });

        return ExitCodes.Success;
    }
}
