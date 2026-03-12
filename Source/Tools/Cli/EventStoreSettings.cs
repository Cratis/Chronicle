// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli;

/// <summary>
/// Settings for commands that operate within a specific event store and namespace.
/// </summary>
public class EventStoreSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [CommandOption("-e|--event-store <NAME>")]
    [Description("Event store name")]
    [DefaultValue("default")]
    public string EventStore { get; set; } = "default";

    /// <summary>
    /// Gets or sets the namespace name.
    /// </summary>
    [CommandOption("-n|--namespace <NAME>")]
    [Description("Namespace within the event store")]
    [DefaultValue("Default")]
    public string Namespace { get; set; } = "Default";

    /// <summary>
    /// Resolves the effective event store name by checking flag, then current context, then default.
    /// </summary>
    /// <returns>The resolved event store name.</returns>
    public string ResolveEventStore()
    {
        if (EventStore != "default")
        {
            return EventStore;
        }

        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();
        if (!string.IsNullOrWhiteSpace(ctx.EventStore))
        {
            return ctx.EventStore;
        }

        return "default";
    }

    /// <summary>
    /// Resolves the effective namespace by checking flag, then current context, then default.
    /// </summary>
    /// <returns>The resolved namespace name.</returns>
    public string ResolveNamespace()
    {
        if (Namespace != "Default")
        {
            return Namespace;
        }

        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();
        if (!string.IsNullOrWhiteSpace(ctx.Namespace))
        {
            return ctx.Namespace;
        }

        return "Default";
    }
}
