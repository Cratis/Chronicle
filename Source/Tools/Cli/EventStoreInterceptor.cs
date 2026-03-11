// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli;

/// <summary>
/// Interceptor that prompts the user to configure a default event store when one is not yet set.
/// Only triggers for commands that use <see cref="EventStoreSettings"/> and only when running interactively.
/// </summary>
public class EventStoreInterceptor : ICommandInterceptor
{
    /// <inheritdoc/>
    public void Intercept(CommandContext context, CommandSettings settings)
    {
        if (settings is not EventStoreSettings eventStoreSettings)
        {
            return;
        }

        // If the user passed --event-store explicitly (not the default), skip prompting.
        if (eventStoreSettings.EventStore != "default")
        {
            return;
        }

        var config = CliConfiguration.Load();

        // If already configured, no need to prompt.
        if (!string.IsNullOrWhiteSpace(config.DefaultEventStore))
        {
            return;
        }

        // Only prompt in interactive terminals.
        if (!AnsiConsole.Profile.Out.IsTerminal)
        {
            return;
        }

        PromptForDefaultEventStore(eventStoreSettings, config);
    }

    /// <summary>
    /// Connects to the Chronicle server, fetches available event stores, and prompts the user to choose one.
    /// </summary>
    /// <param name="settings">The event store settings from the current command.</param>
    /// <param name="config">The CLI configuration to save the selection to.</param>
    static void PromptForDefaultEventStore(EventStoreSettings settings, CliConfiguration config)
    {
        try
        {
            var connectionString = settings.ResolveConnectionString();
            var options = ChronicleOptions.FromConnectionString(new ChronicleConnectionString(connectionString));
            options.AutoDiscoverAndRegister = false;
            using var client = new ChronicleClient(options);

            var eventStores = client.GetEventStores().GetAwaiter().GetResult().Select(e => e.Value).ToList();

            if (eventStores.Count == 0)
            {
                return;
            }

            AnsiConsole.MarkupLine("[yellow]No default event store configured.[/]");

            string selected;

            if (eventStores.Count == 1)
            {
                selected = eventStores[0];
                if (!AnsiConsole.Confirm($"Use [green]{selected.EscapeMarkup()}[/] as default event store?"))
                {
                    return;
                }
            }
            else
            {
                selected = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select a default event store:")
                        .AddChoices(eventStores));
            }

            config.DefaultEventStore = selected;
            config.Save();
            AnsiConsole.MarkupLine($"[green]Default event store set to '{selected.EscapeMarkup()}'.[/]");
            AnsiConsole.MarkupLine("[dim]You can change it later with: cratis config set event-store <name>[/]");
            AnsiConsole.WriteLine();
        }
        catch
        {
            // If we can't connect, silently skip — the actual command will report the connection error.
        }
    }
}
